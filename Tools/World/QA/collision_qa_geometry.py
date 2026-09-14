"""Read actual Unreal primitives and find independent collision QA trajectories.

No collision authoring module, generated spec, or rules are imported. Geometry
helpers remain ordinary Python so their distance/route calculations can be tested.
"""
from __future__ import annotations

import math

TAG = "JongguMigration:collision:"
RADIUS = 24.0


def xyz(v):
    return [float(v.x), float(v.y), float(v.z)]


def add(a, b, scale=1.0):
    return [a[i] + scale * b[i] for i in range(3)]


def sub(a, b):
    return [a[i] - b[i] for i in range(3)]


def dot(a, b):
    return sum(a[i] * b[i] for i in range(3))


def length(v):
    return math.sqrt(dot(v, v))


def normalized(v):
    size = length(v)
    return [x / size for x in v] if size else [0.0, 0.0, 0.0]


def surface_distance(point, shape):
    """Signed point distance to a sphere or oriented box in actual world units."""
    delta = sub(point, shape["center"])
    if shape["kind"] == "sphere":
        return length(delta) - shape["radius"]
    q = [abs(dot(delta, axis)) - extent
         for axis, extent in zip(shape["axes"], shape["extents"])]
    return math.sqrt(sum(max(v, 0.0) ** 2 for v in q)) + min(max(q), 0.0)


def clearance(point, shapes, radius=RADIUS):
    return min((surface_distance(point, shape) - radius for shape in shapes), default=1e20)


def clear_segment(start, end, shapes, margin=2.0, step=8.0):
    delta = sub(end, start)
    count = max(1, math.ceil(length(delta) / step))
    return all(clearance(add(start, delta, i / count), shapes) >= margin
               for i in range(count + 1))


def read_shapes(world, unreal):
    shapes = []
    for actor in unreal.GameplayStatics.get_all_actors_of_class(world, unreal.Actor):
        identity = next((str(t)[len("JongguMigration:"):] for t in actor.tags
                         if str(t).startswith(TAG)), None)
        if identity is None:
            continue
        root = actor.root_component
        shape = {"id": identity, "actor_path": actor.get_path_name(),
                 "tags": [str(t) for t in actor.tags],
                 "center": xyz(root.get_world_location()),
                 "profile": str(root.get_collision_profile_name()),
                 "enabled": str(root.get_collision_enabled()),
                 "pawn_response": str(root.get_collision_response_to_channel(unreal.CollisionChannel.ECC_PAWN)),
                 "parent_path": actor.get_attach_parent_actor().get_path_name()
                 if actor.get_attach_parent_actor() else None,
                 "rotation": [float(v) for v in (root.get_world_rotation().pitch,
                                                 root.get_world_rotation().yaw,
                                                 root.get_world_rotation().roll)]}
        if isinstance(root, unreal.BoxComponent):
            shape.update(kind="box", extents=xyz(root.get_scaled_box_extent()),
                         axes=[xyz(root.get_forward_vector()), xyz(root.get_right_vector()), xyz(root.get_up_vector())])
        elif isinstance(root, unreal.SphereComponent):
            shape.update(kind="sphere", radius=float(root.get_scaled_sphere_radius()))
        else:
            raise RuntimeError("Collision root is not Box/Sphere: " + identity)
        shapes.append(shape)
    return sorted(shapes, key=lambda item: item["id"])


def find_clear_lane(origin, direction, distance, shapes, bounds, margin=4.0):
    """Search a clear finite line; verifies the full player's swept footprint."""
    direction = normalized(direction)
    lo, hi = bounds
    candidates = [origin]
    # Favor spawn-adjacent lanes, keeping tests within the real map footprint.
    for x in range(math.ceil(lo[0] / 75) * 75, math.floor(hi[0] / 75) * 75 + 1, 75):
        for z in range(math.ceil(lo[2] / 75) * 75, math.floor(hi[2] / 75) * 75 + 1, 75):
            candidates.append([float(x), origin[1], float(z)])
    candidates.sort(key=lambda p: length(sub(p, origin)))
    for center in candidates:
        start, end = add(center, direction, -distance / 2), add(center, direction, distance / 2)
        if not all(lo[i] + RADIUS < p[i] < hi[i] - RADIUS for p in (start, end) for i in (0, 2)):
            continue
        if clear_segment(start, end, shapes, margin=margin):
            return start, end
    raise RuntimeError("No independently clear lane for direction " + str(direction))


def exposed_face(shape, shapes, normal, approach=100.0):
    """A free approach toward an actual shape, allowing rotated box faces."""
    normal = normalized(normal)
    if shape["kind"] == "sphere":
        support = shape["radius"]
    else:
        support = sum(abs(dot(normal, axis)) * extent
                      for axis, extent in zip(shape["axes"], shape["extents"]))
    contact = add(shape["center"], normal, support + RADIUS)
    start = add(contact, normal, approach)
    stop = add(contact, normal, 2.5)
    if clear_segment(start, stop, shapes, margin=1.0):
        return {"start": start, "contact": contact, "direction": [-n for n in normal],
                "shape_id": shape["id"], "normal": normal}
    return None


def self_test():
    box = {"kind": "box", "center": [0, 0, 0], "extents": [50, 100, 30],
           "axes": [[1, 0, 0], [0, 1, 0], [0, 0, 1]], "id": "test"}
    assert surface_distance([60, 0, 40], box) == math.sqrt(200)
    assert surface_distance([0, 0, 0], box) == -30
    assert abs(clearance([74, 0, 0], [box])) < 1e-9
    assert not clear_segment([-100, 0, 0], [100, 0, 0], [box])
    assert clear_segment([-100, 0, 70], [100, 0, 70], [box])
    sphere = {"kind": "sphere", "center": [0, 0, 0], "radius": 40, "id": "sphere"}
    assert clearance([64, 0, 0], [sphere]) == 0
    assert exposed_face(box, [box], [1, 0, 0])["contact"] == [74, 0, 0]
    inv = math.sqrt(0.5)
    rotated = dict(box, axes=[[inv, 0, inv], [0, 1, 0], [-inv, 0, inv]])
    assert abs(clearance([74 * inv, 0, 74 * inv], [rotated])) < 1e-9
    print("PASS: independent sphere/OBB signed distance, radius clearance, segment and exposed-face geometry")


if __name__ == "__main__":
    self_test()
