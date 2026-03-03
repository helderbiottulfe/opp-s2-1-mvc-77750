using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CommunityLibrary.Models;
using Bogus;

namespace CommunityLibrary.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        await using var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());
        
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        // First, ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Seed Admin Role and User
        await SeedAdminUserAsync(roleManager, userManager);
        
        // Seed Books, Members, Loans
        await SeedLibraryDataAsync(context);
    }
    
    private static async Task SeedAdminUserAsync(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
        Console.WriteLine("Seeding admin user...");
        
        // Create Admin role if it doesn't exist
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            Console.WriteLine("Admin role created");
        }
        
        // Create Admin user if it doesn't exist
        var adminEmail = "admin@library.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("Admin user created");
            }
            else
            {
                Console.WriteLine("Failed to create admin user:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"- {error.Description}");
                }
            }
        }
        else
        {
            Console.WriteLine("Admin user already exists");
        }
    }
    
    private static async Task SeedLibraryDataAsync(ApplicationDbContext context)
    {
        Console.WriteLine("Seeding library data...");
        
        // Check if data already exists
        if (await context.Books.AnyAsync() || await context.Members.AnyAsync())
        {
            Console.WriteLine("Library data already exists, skipping seed");
            return;
        }
        
        // Seed Books using Bogus
        var bookFaker = new Faker<Book>()
            .RuleFor(b => b.Title, f => f.Commerce.ProductName())
            .RuleFor(b => b.Author, f => f.Name.FullName())
            .RuleFor(b => b.Isbn, f => f.Commerce.Ean13())
            .RuleFor(b => b.Category, f => f.PickRandom(new[] { 
                "Fiction", "Non-Fiction", "Science", "History", 
                "Biography", "Children", "Technology", "Art",
                "Mystery", "Romance", "Self-Help" 
            }))
            .RuleFor(b => b.IsAvailable, f => f.Random.Bool(0.7f));
        
        var books = bookFaker.Generate(20);
        await context.Books.AddRangeAsync(books);
        await context.SaveChangesAsync();
        Console.WriteLine($"Added {books.Count} books");
        
        // Seed Members using Bogus
        var memberFaker = new Faker<Member>()
            .RuleFor(m => m.FullName, f => f.Name.FullName())
            .RuleFor(m => m.Email, f => f.Internet.Email())
            .RuleFor(m => m.Phone, f => f.Phone.PhoneNumber("###-###-####"));
        
        var members = memberFaker.Generate(10);
        await context.Members.AddRangeAsync(members);
        await context.SaveChangesAsync();
        Console.WriteLine($"Added {members.Count} members");
        
        // Now that books and members have IDs, seed Loans
        await SeedLoansAsync(context, books, members);
    }
    
    private static async Task SeedLoansAsync(ApplicationDbContext context, List<Book> books, List<Member> members)
    {
        var loans = new List<Loan>();
        var random = new Random();
        
        // Create 15 loans
        for (int i = 0; i < 15; i++)
        {
            var book = books[random.Next(books.Count)];
            var member = members[random.Next(members.Count)];
            
            // Create loan with various scenarios
            var loanDate = DateTime.Now.AddDays(-random.Next(1, 60));
            var dueDate = loanDate.AddDays(14);
            DateTime? returnedDate = null;
            
            // Scenarios:
            // 40% returned, 30% active, 30% overdue
            var scenario = random.Next(1, 101);
            
            if (scenario <= 40) // Returned
            {
                returnedDate = dueDate.AddDays(random.Next(-5, 5)); // Returned around due date
                book.IsAvailable = true;
                Console.WriteLine($"Creating RETURNED loan for book: {book.Title}");
            }
            else if (scenario <= 70) // Active (not overdue)
            {
                dueDate = DateTime.Now.AddDays(random.Next(1, 14)); 
                book.IsAvailable = false;
                Console.WriteLine($"Creating ACTIVE loan for book: {book.Title}");
            }
            else // Overdue
            {
                dueDate = DateTime.Now.AddDays(-random.Next(1, 30)); 
                book.IsAvailable = false;
                Console.WriteLine($"Creating OVERDUE loan for book: {book.Title}");
            }
            
            var loan = new Loan
            {
                BookId = book.Id,
                MemberId = member.Id,
                LoanDate = loanDate,
                DueDate = dueDate,
                ReturnedDate = returnedDate
            };
            
            loans.Add(loan);
        }
        
        await context.Loans.AddRangeAsync(loans);
        await context.SaveChangesAsync();
        Console.WriteLine($"Added {loans.Count} loans");
        
        // Update book availability based on loans
        await context.SaveChangesAsync();
        Console.WriteLine("Library data seeding completed!");
    }
}