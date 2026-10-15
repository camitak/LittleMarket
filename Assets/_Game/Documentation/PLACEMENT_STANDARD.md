# Little Market Placement Standard

## World axes

- +X = Right
- +Y = Up
- +Z = Front
- 1 Unity unit = 1 metre

## Product prefabs

Gameplay prefab:
- Root scale = (1, 1, 1)
- Root uses Little Market orientation
- Visual child absorbs imported-model rotation, pivot and scale differences
- PlacementAnchor is bottom-center of the physical footprint
- PlacementAnchor rotation = identity
- PlacementAnchor +Z points toward the product's visible front

Do not use ProductData shelf offsets to repair bad asset pivots.

## Shelf placement

ShelfSlot has a DisplayOrigin.

DisplayOrigin:
- lies exactly on the physical shelf surface
- +Y points up
- +Z points toward the aisle / direction the product should face

Product PlacementAnchor is aligned to DisplayOrigin.

ProductData shelfDisplayOffset is only for intentional merchandising layout.

## Delivery cartons

DeliveryBox has a bottom-center PlacementAnchor.

DeliveryZone points represent the floor/contact surface.

Delivery point +Z determines the carton's front direction.

## Furniture

For newly authored furniture:
- root is on the floor
- +Z is the main interaction/customer-facing side
- +Y is up
- scale is (1, 1, 1)

Rotate the whole prefab instance in the scene instead of rotating all internal children.

## Imported assets

Never change Little Market's coordinate convention to match an imported model.

Instead:
1. Put imported geometry below a Visual transform.
2. Rotate Visual until the object's front faces +Z.
3. Scale Visual to metres.
4. Position the PlacementAnchor at bottom-center.
5. Configure collider(s).
6. Verify in Front and Right orthographic Scene views.