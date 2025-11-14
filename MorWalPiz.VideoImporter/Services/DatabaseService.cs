using Microsoft.EntityFrameworkCore;
using MorWalPiz.VideoImporter.Data;
using System.IO;

namespace MorWalPiz.VideoImporter.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly ITenantContext _tenantContext;

        public DatabaseService(ITenantContext tenantContext)
        {
            _dbPath = Path.Combine(Directory.GetCurrentDirectory(), "VideoImporter.db");
            _tenantContext = tenantContext;
        }

        public void InitializeDatabase()
        {
            // Opzionalmente, elimina il database se esiste già
            // Questo assicura che tutte le nuove entità vengano aggiunte
            // Nota: rimuovi queste righe in produzione per evitare la perdita di dati
            //if (File.Exists(_dbPath))
            //{
            //    File.Delete(_dbPath);
            //}

            // Crea un contesto temporaneo per inizializzare il database
            using (var context = new AppDbContext(_tenantContext))
            {
                // Assicura che il database esista e che sia aggiornato allo schema più recente
                // Migrate() creerà il database se non esiste e applicherà tutte le migrazioni pendenti
                //context.Database.Migrate();
            }
        }

        /// <summary>
        /// Crea un nuovo contesto da utilizzare all'interno di un blocco using.
        /// Questo garantisce che ogni operazione abbia un proprio contesto isolato.
        /// </summary>
        public AppDbContext CreateContext()
        {
            return new AppDbContext(_tenantContext);
        }
    }
}
