# Affiliate Catalog Channel Backfill

`Product` and `ProductCategory` now require a persisted `channelId` for all
BackOffice and public reads. BSON deserialization remains tolerant so existing
documents can be inspected, but documents without this field are intentionally
excluded from scoped queries.

Before deploying the channel-scoped API, manually assign each existing affiliate
document to its owning YouTube channel in MongoDB. Review the mapping with the
content owner; do not infer ownership from the current ServerAPI configuration
when multiple channels are present.

Example review and update commands (replace the values after verification):

```javascript
db.products.find({ channelId: { $exists: false } }, { _id: 1, title: 1 })
db.productCategories.find({ channelId: { $exists: false } }, { _id: 1, title: 1 })

db.products.updateMany(
  { _id: { $in: [/* verified product ids */] } },
  { $set: { channelId: "<verified-channel-id>" } }
)
db.productCategories.updateMany(
  { _id: { $in: [/* verified category ids */] } },
  { $set: { channelId: "<verified-channel-id>" } }
)
```

After backfill, verify that every `CategoryRef` on a product points to a
category with the same `channelId`, and that titles are unique within each
channel. Product mutations invalidate the public `tag-product` cache after the
backfill.

This procedure does not apply to `DigitalProduct` or
`DigitalProductCategory`, which remain shop-owned.