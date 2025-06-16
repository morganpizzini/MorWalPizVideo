using MorWalPiz.VideoImporter.Data;
using System.IO;

namespace MorWalPiz.VideoImporter.Services
{
  public class DatabaseService
  {
    private readonly string _dbPath;

    public DatabaseService()
    {
      _dbPath = Path.Combine(Directory.GetCurrentDirectory(), "VideoImporter.db");
    }

    public void InitializeDatabase()
    {
      // Opzionalmente, elimina il database se esiste già
      // Questo assicura che tutte le nuove entità vengano aggiunte
      // Nota: rimuovi queste righe in produzione per evitare la perdita di dati
      //if (File.Exists(_dbPath))
      //{
      //  File.Delete(_dbPath);
      //}

      // Crea un contesto temporaneo per inizializzare il database
      using (var context = new AppDbContext())
      {
        // Assicura che il database esista e che sia aggiornato allo schema più recente
        context.Database.EnsureCreated();
      }
    }

    /// <summary>
    /// Crea un nuovo contesto da utilizzare all'interno di un blocco using.
    /// Questo garantisce che ogni operazione abbia un proprio contesto isolato.
    /// </summary>
    public AppDbContext CreateContext()
    {
      return new AppDbContext();
    }
  }
}