const targets = [
  { collection: "workshopRecipes", field: "drawing" },
  { collection: "workshopRecipeVersions", field: "snapshot.drawing" },
  { collection: "workshopProductionSessions", field: "recipeSnapshot.drawing" },
  { collection: "workshopAmmoBatches", field: "recipeSnapshot.drawing" },
];

const apply = process.argv.includes("--apply");
const confirm = process.argv.includes("--confirm-drawing-removal");

if (db.getName() === "admin" || db.getName() === "config" || db.getName() === "local") {
  throw new Error(`Refusing to run against system database '${db.getName()}'.`);
}

if (apply && !confirm) {
  throw new Error("Applying this destructive migration requires --confirm-drawing-removal.");
}

print(`Database: ${db.getName()}`);
print(apply
  ? "APPLY mode: removing Drawing fields. Confirm writes were stopped and a verified backup exists."
  : "DRY RUN: no documents will be changed. Pass --apply --confirm-drawing-removal only after backup and write freeze.");

for (const target of targets) {
  const collection = db.getCollection(target.collection);
  const filter = { [target.field]: { $exists: true } };
  const before = collection.countDocuments(filter);

  if (apply && before > 0) {
    collection.updateMany(filter, { $unset: { [target.field]: "" } });
  }

  const remaining = collection.countDocuments(filter);
  print(`${target.collection}.${target.field}: before=${before}, remaining=${remaining}`);

  if (apply && remaining !== 0) {
    throw new Error(`Drawing contraction incomplete for ${target.collection}.${target.field}.`);
  }
}
