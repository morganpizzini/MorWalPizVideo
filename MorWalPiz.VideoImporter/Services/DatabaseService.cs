using MorWalPiz.VideoImporter.Data;
using System.IO;

namespace MorWalPiz.VideoImporter.Services
{
  public class DatabaseService
  {
    private readonly AppDbContext _context;

    public DatabaseService()
    {
      _context = new AppDbContext();
    }

    public void InitializeDatabase()
    {
      // Opzionalmente, elimina il database se esiste già
      // Questo assicura che tutte le nuove entità vengano aggiunte
      // Nota: rimuovi queste righe in produzione per evitare la perdita di dati
      string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "VideoImporter.db");
      if (File.Exists(dbPath))
      {
        File.Delete(dbPath);
      }

      // Assicura che il database esista e che sia aggiornato allo schema più recente
      _context.Database.EnsureCreated();
    }

    public AppDbContext GetContext()
    {
      return _context;
    }
  }
}