using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CommunityLibrary.Data;
using CommunityLibrary.Models;

namespace CommunityLibrary.Controllers;

[Authorize]
public class BooksController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public BooksController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    // GET: Books with search and filter
    public async Task<IActionResult> Index(string searchString, string category, string availability)
    {
        // Start with IQueryable - important for database-side filtering
        var booksQuery = _context.Books.AsQueryable();
        
        // Search by Title or Author
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            booksQuery = booksQuery.Where(b => 
                b.Title.Contains(searchString) || 
                b.Author.Contains(searchString));
        }
        
        // Filter by Category
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            booksQuery = booksQuery.Where(b => b.Category == category);
        }
        
        // Filter by Availability
        if (!string.IsNullOrWhiteSpace(availability) && availability != "All")
        {
            if (availability == "Available")
            {
                booksQuery = booksQuery.Where(b => b.IsAvailable == true);
            }
            else if (availability == "OnLoan")
            {
                booksQuery = booksQuery.Where(b => b.IsAvailable == false);
            }
        }
        
        // Get distinct categories for dropdown
        var categories = await _context.Books
            .Select(b => b.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
        
        // Store current filter values in ViewBag
        ViewBag.CurrentSearch = searchString;
        ViewBag.CurrentCategory = category ?? "All";
        ViewBag.CurrentAvailability = availability ?? "All";
        ViewBag.Categories = categories;
        
        // Execute the query and get results
        var books = await booksQuery.ToListAsync();
        
        return View(books);
    }
    
    // GET: Books/Create
    public IActionResult Create()
    {
        return View();
    }
    
    // POST: Books/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Author,Isbn,Category")] Book book)
    {
        if (ModelState.IsValid)
        {
            book.IsAvailable = true;
            _context.Add(book);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(book);
    }
    
    // GET: Books/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        
        var book = await _context.Books.FindAsync(id);
        if (book == null) return NotFound();
        
        return View(book);
    }
    
    // POST: Books/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Author,Isbn,Category,IsAvailable")] Book book)
    {
        if (id != book.Id) return NotFound();
        
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(book);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(book.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(book);
    }
    
    // GET: Books/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        
        var book = await _context.Books.FindAsync(id);
        if (book == null) return NotFound();
        
        return View(book);
    }
    
    // POST: Books/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book != null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }
        
        return RedirectToAction(nameof(Index));
    }
    
    private bool BookExists(int id)
    {
        return _context.Books.Any(e => e.Id == id);
    }
}