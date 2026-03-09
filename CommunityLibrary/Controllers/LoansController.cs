using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CommunityLibrary.Data;
using CommunityLibrary.Models;

namespace CommunityLibrary.Controllers;

[Authorize]
public class LoansController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public LoansController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    // GET: Loans
    public async Task<IActionResult> Index()
    {
        var loans = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .OrderByDescending(l => l.LoanDate)
            .ToListAsync();
        
        return View(loans);
    }
    
    // GET: Loans/Create
    public async Task<IActionResult> Create()
    {
        // Only show available books
        ViewBag.Books = await _context.Books
            .Where(b => b.IsAvailable)
            .OrderBy(b => b.Title)
            .ToListAsync();
        
        ViewBag.Members = await _context.Members
            .OrderBy(m => m.FullName)
            .ToListAsync();
        
        return View();
    }
    
    // POST: Loans/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int bookId, int memberId)
    {
        // Check if book is available
        var book = await _context.Books.FindAsync(bookId);
        if (book == null || !book.IsAvailable)
        {
            ModelState.AddModelError("", "This book is not available for loan.");
            return await ReloadCreateView();
        }
        
        var loan = new Loan
        {
            BookId = bookId,
            MemberId = memberId,
            LoanDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(14),
            ReturnedDate = null
        };
        
        // Mark book as unavailable
        book.IsAvailable = false;
        
        _context.Add(loan);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    
    // POST: Loans/Return/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id)
    {
        var loan = await _context.Loans
            .Include(l => l.Book)
            .FirstOrDefaultAsync(l => l.Id == id);
        
        if (loan != null && loan.ReturnedDate == null)
        {
            loan.ReturnedDate = DateTime.Now;
            loan.Book.IsAvailable = true;
            await _context.SaveChangesAsync();
        }
        
        return RedirectToAction(nameof(Index));
    }
    
    // GET: Loans/Overdue
    public async Task<IActionResult> Overdue()
    {
        var overdueLoans = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .Where(l => l.DueDate < DateTime.Now && l.ReturnedDate == null)
            .OrderBy(l => l.DueDate)
            .ToListAsync();
        
        return View(overdueLoans);
    }
    
    private async Task<IActionResult> ReloadCreateView()
    {
        ViewBag.Books = await _context.Books
            .Where(b => b.IsAvailable)
            .OrderBy(b => b.Title)
            .ToListAsync();
        
        ViewBag.Members = await _context.Members
            .OrderBy(m => m.FullName)
            .ToListAsync();
        
        return View("Create");
    }
}
