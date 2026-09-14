"""Reconcile only game-owned overlay actors, preserving source identities and geometry."""

OWN_TAG = 'JongguPrototype'
ID_TAG = 'JongguPrototypeId:'
INTERACTION_IDS = {
    'hub_menu': 0, 'hub_pan': 1, 'hub_pot': 2, 'hub_discard': 3,
    'hub_beach_portal': 4, 'hub_seat_1': 5, 'hub_seat_2': 6,
    'beach_hub_portal': 7, 'beach_clam_1': 8, 'beach_clam_2': 9,
    'beach_clam_3': 10,
}

INTERACTION_LABELS = {
    'hub_menu': 'MenuBoard', 'hub_pan': 'CookingPan', 'hub_pot': 'CookingPot',
    'hub_discard': 'DishDiscard', 'hub_beach_portal': 'TravelToBeach',
    'hub_seat_1': 'ServiceSeat_01', 'hub_seat_2': 'ServiceSeat_02',
    'beach_hub_portal': 'TravelToRestaurant', 'beach_clam_1': 'ClamGather_01',
    'beach_clam_2': 'ClamGather_02', 'beach_clam_3': 'ClamGather_03',
}


def actor_presentation(key, map_name):
    """Readable Outliner presentation; the persistent ownership tags stay stable."""
    scene = 'Restaurant' if map_name == 'Hub' else 'Beach'
    if key == 'manager':
        return scene + 'Manager', 'Gameplay/Managers'
    if key.startswith('marker:'):
        return INTERACTION_LABELS[key[len('marker:'):]], 'Gameplay/' + scene + '/Interactions'
    if key.startswith('customer:seat_'):
        seat = int(key[len('customer:seat_'):])
        return 'Customer_%02d' % seat, 'Gameplay/Restaurant/Customers'
    raise ValueError('Unknown game-owned actor identity: ' + key)


def _source_snapshot(actors):
    result = {}
    for actor in actors:
        tags = [str(t) for t in actor.tags]
        identity = next((t for t in tags if t.startswith('JongguMigration:')), None)
        if identity is None: continue
        transform = actor.get_actor_transform()
        result[identity] = (
            actor.get_class().get_path_name(), tuple(tags),
            tuple(float(getattr(transform.translation, k)) for k in ('x', 'y', 'z')),
            tuple(float(getattr(transform.rotation, k)) for k in ('x', 'y', 'z', 'w')),
            tuple(float(getattr(transform.scale3d, k)) for k in ('x', 'y', 'z')),
            bool(actor.get_editor_property('hidden')))
    return result


def _upsert(actors_api, cls, key, position, actors, map_name):
    import unreal
    uclass = cls.static_class() if hasattr(cls, 'static_class') and not isinstance(cls, unreal.Class) else cls
    matches = [a for a in actors if OWN_TAG in [str(t) for t in a.tags] and ID_TAG + key in [str(t) for t in a.tags]]
    if key == 'manager' and not matches:
        matches = [a for a in actors if a.get_class() == uclass and not any(str(t).startswith('JongguMigration:') for t in a.tags)]
    actor = matches[0] if matches else None
    for duplicate in matches[1:]:
        if not actors_api.destroy_actor(duplicate): raise RuntimeError('Cannot remove duplicate prototype ' + key)
    if actor is not None and actor.get_class() != uclass:
        if not actors_api.destroy_actor(actor): raise RuntimeError('Cannot replace prototype ' + key)
        actor = None
    if actor is None:
        actor = actors_api.spawn_actor_from_class(cls, unreal.Vector(*position))
        if actor is None: raise RuntimeError('Cannot spawn prototype ' + key)
    actor.tags = [OWN_TAG, ID_TAG + key]
    label, folder = actor_presentation(key, map_name)
    actor.set_actor_label(label, mark_dirty=True)
    actor.set_folder_path(folder)
    actor.set_actor_location(unreal.Vector(*position), False, True)
    actor.set_actor_enable_collision(False)
    actor.set_actor_hidden_in_game(False)
    return actor
