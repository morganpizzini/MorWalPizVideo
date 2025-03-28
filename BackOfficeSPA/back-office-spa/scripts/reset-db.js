import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

// Get the current directory
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Paths to the database files
const defaultDbPath = path.join(__dirname, '..', 'db.default.json');
const dbPath = path.join(__dirname, '..', 'db.json');

// Read the default database content
const defaultDb = fs.readFileSync(defaultDbPath, 'utf8');

// Write the default content to the active database file
fs.writeFileSync(dbPath, defaultDb);

console.log('Database reset to default state successfully!');
