# Stage Designer Schema

The exported and localStorage payload remains JSON version `1.0` under the `ipsc-stages` key.

Stages may now include `widthMeters` and `heightMeters` as positive numbers. Missing or invalid values are normalized to `null`; the stage content remains intact and the boundary stays disabled until both values are supplied. Missing `drawings`, `fills`, `objects`, and `measurements` arrays are normalized to empty arrays. Legacy target visibility values `halfSize` and `bothSides` are normalized to `centerOnly` when loaded.
