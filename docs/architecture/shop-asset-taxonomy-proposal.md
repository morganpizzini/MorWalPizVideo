# Shop Asset Taxonomy Proposal

This is an initial editorial proposal only. It is not a persisted schema, migration, or validation constraint; the definitive taxonomy remains a product decision after implementation.

Suggested top-level groups:

- **Templates**: reusable document, planning, and production templates.
- **Guides**: practical guides, checklists, and reference sheets.
- **Presets**: configuration, editing, and workflow presets.
- **Bundles**: curated sets of related artifacts sold as one order item.
- **Educational**: lessons, workbooks, and exercises.

Each future asset may additionally receive optional facets such as topic, audience, format, language, version, and license. These facets should remain additive and should not be encoded as irreversible Mongo fields until the owner confirms the final vocabulary.