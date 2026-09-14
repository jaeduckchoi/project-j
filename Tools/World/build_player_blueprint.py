"""Build a portable Paper2D player using UE5.8's compiled Blueprint graph API.

Python runs only while authoring. The saved Pawn, components and Blueprint graphs
provide movement and animation without a project C++ module or runtime Python.
"""
from __future__ import annotations

import json
from pathlib import Path
import sys

import unreal

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))
from world_paths import CONTENT_ROOT, QA_ROOT, require_unreal_project

PLAYER_PATH = CONTENT_ROOT + "/Blueprints/Player/BP_JongguPlayer"
VERSION = "blueprint-player-walk-9-input-facing"
B = unreal.BlueprintEditorLibrary
ASSETS = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
TOOLS = unreal.AssetToolsHelpers.get_asset_tools()
MATH = "/Script/Engine.KismetMathLibrary."


class Graph:
    def __init__(self, editor):
        self.editor = editor
        self.nodes = []

    def place(self, node):
        if node is None:
            raise RuntimeError("Blueprint node creation failed")
        index = len(self.nodes)
        node.set_node_pos(unreal.IntPoint((index % 7) * 300, (index // 7) * 190))
        self.nodes.append(node)
        return node

    @staticmethod
    def connect(source, target):
        if not source.is_valid() or not target.is_valid() or not source.try_create_connection(target):
            raise RuntimeError("Cannot connect Blueprint pins: %s -> %s" % (source, target))

    @staticmethod
    def literal(pin, value):
        text = str(value).lower() if isinstance(value, bool) else str(value)
        if not pin.is_valid() or not pin.set_pin_value(text):
            raise RuntimeError("Cannot set Blueprint pin %s to %s" % (pin, text))

    def function(graph, path, **inputs):
        # "self" is an actual Blueprint input pin, so reserve that keyword for inputs.
        node = graph.place(graph.editor.add_call_function_node(path))
        for name, value in inputs.items():
            pin = node.find_input_pin(name)
            if isinstance(value, unreal.BlueprintGraphPin):
                graph.connect(value, pin)
            else:
                graph.literal(pin, value)
        return node

    def math(self, name, **inputs):
        return self.function(MATH + name, **inputs).find_output_pin("ReturnValue")

    def get(self, name):
        return self.place(self.editor.add_get_member_variable_node(name)).find_output_pin(name)

    def property(self, obj, cls, name):
        node = self.place(self.editor.add_get_member_variable_node(name, cls))
        self.connect(obj, node.find_input_pin("self"))
        return node.find_output_pin(name)

    def set(self, name, value):
        node = self.place(self.editor.add_set_member_variable_node(name))
        self.connect(value, node.find_input_pin(name))
        return node

    def flow(self, *nodes):
        for source, target in zip(nodes, nodes[1:]):
            pin = source if isinstance(source, unreal.BlueprintGraphPin) else source.find_then_pin()
            self.connect(pin, target.find_execute_pin())


def compile_checked(bp):
    if not B.compile_blueprint(bp):
        errors = []
        for graph in B.list_graphs(bp):
            editor = unreal.BlueprintGraphEditor.get_graph_editor(graph)
            errors.extend(str(n) for n in editor.list_nodes_with_errors())
        raise RuntimeError("Player Blueprint did not compile: " + "\n".join(errors))


def create_components(bp):
    subsystem = unreal.get_engine_subsystem(unreal.SubobjectDataSubsystem)
    data_lib = unreal.SubobjectDataBlueprintFunctionLibrary
    handles = subsystem.k2_gather_subobject_data_for_blueprint(bp)
    context = handles[0]

    def add(name, cls, parent):
        handle, reason = subsystem.add_new_subobject(unreal.AddNewSubobjectParams(
            parent_handle=parent, new_class=cls, blueprint_context=bp))
        if str(reason):
            raise RuntimeError("Cannot add player component %s: %s" % (name, reason))
        subsystem.rename_subobject_member_variable(bp, handle, name)
        data = data_lib.get_data(handle)
        return handle, data_lib.get_object_for_blueprint(data, bp)

    sphere_handle, sphere = add("collision_component", unreal.SphereComponent, context)
    if not subsystem.make_new_scene_root(context, sphere_handle, bp):
        raise RuntimeError("Cannot make player collision the scene root")
    sphere.set_sphere_radius(24.0, False)
    sphere.set_collision_profile_name("Pawn")
    sphere.set_mobility(unreal.ComponentMobility.MOVABLE)
    sphere.set_editor_property("can_ever_affect_navigation", False)
    _, visual = add("player_visual", unreal.PaperSpriteComponent, sphere_handle)
    visual.set_mobility(unreal.ComponentMobility.MOVABLE)
    visual.set_collision_enabled(unreal.CollisionEnabled.NO_COLLISION)
    visual.set_editor_property("cast_shadow", False)
    visual.set_editor_property("generate_overlap_events", False)
    visual.set_relative_scale3d(unreal.Vector(1.67, 1.0, 1.67))
    visual.set_translucent_sort_priority(12)
    _, movement = add("movement_component", unreal.FloatingPawnMovement, context)
    movement.set_editor_property("max_speed", 400.0)
    movement.set_editor_property("acceleration", 3200.0)
    movement.set_editor_property("deceleration", 4800.0)
    movement.set_editor_property("turning_boost", 12.0)
    movement.set_plane_constraint_enabled(True)
    movement.set_plane_constraint_normal(unreal.Vector(0, 1, 0))


def add_variables(bp, editor):
    sprite_array = B.get_array_type(B.get_object_reference_type(unreal.PaperSprite))
    vector_type = B.get_struct_type(unreal.load_object(None, "/Script/CoreUObject.Vector"))
    definitions = [(d + "_frames", sprite_array, "") for d in ("front", "back", "side")]
    definitions += [(d + "_walk_parts", sprite_array, "") for d in ("front", "back", "side")]
    definitions += [
        ("move_speed", B.get_basic_type_by_name("real"), "400.0"),
        ("idle_frames_per_second", B.get_basic_type_by_name("real"), str(1.0 / 0.3)),
        ("side_sprite_faces_left", B.get_basic_type_by_name("bool"), "true"),
        ("visual_scale", vector_type, "(X=1.67,Y=1.0,Z=1.67)"),
        ("facing_direction", B.get_basic_type_by_name("int"), "0"),
        ("is_moving", B.get_basic_type_by_name("bool"), "false"),
        ("current_frame_index", B.get_basic_type_by_name("int"), "0"),
        ("walk_cycle_distance", B.get_basic_type_by_name("real"), "160.0"),
        ("walk_bob_cm", B.get_basic_type_by_name("real"), "1.0"),
        ("walk_lean_degrees", B.get_basic_type_by_name("real"), "1.5"),
        ("walk_phase", B.get_basic_type_by_name("real"), "0.0"),
        ("walk_blend", B.get_basic_type_by_name("real"), "0.0"),
        ("visual_lean", B.get_basic_type_by_name("real"), "0.0"),
        ("visual_origin", vector_type, "(X=0,Y=0,Z=0)"),
        ("previous_position", vector_type, "(X=0,Y=0,Z=0)"),
        ("idle_elapsed", B.get_basic_type_by_name("real"), "0.0"),
    ]
    existing = set(B.list_member_variable_names(bp, False))
    for name, pin_type, default in definitions:
        if name not in existing and not editor.add_member_variable(name, pin_type, default):
            raise RuntimeError("Cannot create player variable " + name)
        state = name in {"walk_phase", "walk_blend", "visual_lean", "visual_origin", "previous_position", "idle_elapsed", "is_moving", "facing_direction", "current_frame_index"}
        # These original three states are authored explicitly by the scene
        # importer; preserve that existing serialization/editing contract.
        B.set_blueprint_variable_instance_editable(bp, name, not state or name in {"facing_direction", "is_moving", "current_frame_index"})
        B.set_blueprint_variable_category(bp, name, "Player|Motion state" if state else "Player")


def ensure_walk_components(bp):
    subsystem = unreal.get_engine_subsystem(unreal.SubobjectDataSubsystem)
    library = unreal.SubobjectDataBlueprintFunctionLibrary
    handles = subsystem.k2_gather_subobject_data_for_blueprint(bp)
    objects = [(handle, library.get_object_for_blueprint(library.get_data(handle), bp)) for handle in handles]
    visual_handle = next(handle for handle, obj in objects if obj and obj.get_name().startswith("player_visual"))
    for _, obj in objects:
        if isinstance(obj, unreal.FloatingPawnMovement):
            obj.set_editor_property("acceleration", 3200.0)
            obj.set_editor_property("deceleration", 4800.0)
            obj.set_editor_property("turning_boost", 12.0)
    names = [obj.get_name() for _, obj in objects if obj]
    for name in ("walk_head", "walk_body", "walk_left_foot", "walk_right_foot"):
        if any(existing.startswith(name) for existing in names):
            continue
        handle, reason = subsystem.add_new_subobject(unreal.AddNewSubobjectParams(parent_handle=visual_handle, new_class=unreal.PaperSpriteComponent, blueprint_context=bp))
        if str(reason):
            raise RuntimeError("Cannot create walk part: " + str(reason))
        subsystem.rename_subobject_member_variable(bp, handle, name)
        part = library.get_object_for_blueprint(library.get_data(handle), bp)
        part.set_mobility(unreal.ComponentMobility.MOVABLE)
        part.set_collision_enabled(unreal.CollisionEnabled.NO_COLLISION)
        part.set_cast_shadow(False)
        part.set_editor_property("generate_overlap_events", False)
        part.set_visibility(False, False)


def build_refresh_graph(bp):
    editor = unreal.BlueprintGraphEditor.get_graph_editor(B.find_graph(bp, "RefreshPlayerVisual"))
    editor.set_function_is_public()
    editor.set_is_call_in_editor_function(False)
    graph = Graph(editor)
    entry = editor.find_graph_entry_pin()
    velocity = graph.function("/Script/Engine.Actor.GetVelocity").find_output_pin("ReturnValue")
    dt = graph.function("/Script/Engine.GameplayStatics.GetWorldDeltaSeconds").find_output_pin("ReturnValue")
    position = graph.function("/Script/Engine.Actor.K2_GetActorLocation").find_output_pin("ReturnValue")
    distance = graph.math("VSize", A=graph.math("Subtract_VectorVector", A=position, B=graph.get("previous_position")))
    # Ignore teleports; phase follows travelled ground distance, not global time.
    max_step = graph.math("FMax", A=20.0, B=graph.math("Multiply_DoubleDouble", A=graph.get("move_speed"), B=graph.math("Multiply_DoubleDouble", A=dt, B=4.0)))
    distance = graph.math("SelectFloat", A=0.0, B=distance, bPickA=graph.math("Greater_DoubleDouble", A=distance, B=max_step))
    components = graph.function(MATH + "BreakVector", InVec=velocity)
    vx, vz = components.find_output_pin("X"), components.find_output_pin("Z")
    # Collision may remove the requested velocity or leave only a tangent.
    # Face input intent while walk/idle and travelled distance follow real motion.
    # Pending input catches this actor tick's keyboard input; the last consumed
    # vector covers native MovementComponent's earlier tick and injected input.
    pending_input = graph.function("/Script/Engine.Pawn.GetPendingMovementInputVector").find_output_pin("ReturnValue")
    last_input = graph.function("/Script/Engine.Pawn.GetLastMovementInputVector").find_output_pin("ReturnValue")
    has_pending = graph.math("Greater_DoubleDouble", A=graph.math("VSizeSquared", A=pending_input), B=0.0001)
    intent = graph.math("SelectVector", A=pending_input, B=last_input, bPickA=has_pending)
    has_intent = graph.math("Greater_DoubleDouble", A=graph.math("VSizeSquared", A=intent), B=0.0001)
    intent_components = graph.function(MATH + "BreakVector", InVec=intent)
    ix, iz = intent_components.find_output_pin("X"), intent_components.find_output_pin("Z")
    moving = graph.math("Greater_DoubleDouble", A=graph.math("VSizeSquared", A=velocity), B=1.0)
    set_moving = graph.set("is_moving", moving)
    new_side = graph.math("SelectInt", A=3, B=2, bPickA=graph.math("Greater_DoubleDouble", A=ix, B=0.0))
    new_vertical = graph.math("SelectInt", A=1, B=0, bPickA=graph.math("Greater_DoubleDouble", A=iz, B=0.0))
    # A 15% diagonal band keeps the previous compatible facing. From an
    # incompatible facing, use the horizontal view so the feet stay readable.
    ax, az = graph.math("Abs", A=ix), graph.math("Abs", A=iz)
    strong_horizontal = graph.math("Greater_DoubleDouble", A=ax, B=graph.math("Multiply_DoubleDouble", A=az, B=1.15))
    strong_vertical = graph.math("Greater_DoubleDouble", A=az, B=graph.math("Multiply_DoubleDouble", A=ax, B=1.15))
    keep_vertical = graph.math("EqualEqual_IntInt", A=graph.get("facing_direction"), B=new_vertical)
    horizontal = graph.math("BooleanOR", A=strong_horizontal, B=graph.math("BooleanAND", A=graph.math("Not_PreBool", A=strong_vertical), B=graph.math("Not_PreBool", A=keep_vertical)))
    new_facing = graph.math("SelectInt", A=new_side, B=new_vertical, bPickA=horizontal)
    facing = graph.math("SelectInt", A=new_facing, B=graph.get("facing_direction"), bPickA=has_intent)
    set_facing = graph.set("facing_direction", facing)
    advance = graph.math("Divide_DoubleDouble", A=distance, B=graph.math("FMax", A=graph.get("walk_cycle_distance"), B=1.0))
    phase = graph.math("Fraction", A=graph.math("Add_DoubleDouble", A=graph.get("walk_phase"), B=advance))
    just_started = graph.math("BooleanAND", A=moving, B=graph.math("Not_PreBool", A=graph.get("is_moving")))
    set_phase = graph.set("walk_phase", graph.math("SelectFloat", A=0.0, B=phase, bPickA=just_started))
    set_position = graph.set("previous_position", position)
    elapsed = graph.math("Add_DoubleDouble", A=graph.get("idle_elapsed"), B=dt)
    set_elapsed = graph.set("idle_elapsed", graph.math("SelectFloat", A=0.0, B=elapsed, bPickA=moving))
    blend = graph.math("FInterpTo", Current=graph.get("walk_blend"), Target=graph.math("SelectFloat", A=1.0, B=0.0, bPickA=moving), DeltaTime=dt, InterpSpeed=18.0)
    set_blend = graph.set("walk_blend", blend)
    fps = graph.math("FMax", A=graph.get("idle_frames_per_second"), B=0.01)
    floor = graph.math("FFloor", A=graph.math("Multiply_DoubleDouble", A=graph.get("idle_elapsed"), B=fps))
    idle_index = graph.math("Percent_IntInt", A=floor, B=2)
    walk_index = graph.math("Clamp", Value=graph.math("FFloor", A=graph.math("Multiply_DoubleDouble", A=graph.get("walk_phase"), B=8.0)), Min=0, Max=7)
    frame = graph.math("SelectInt", A=walk_index, B=idle_index, bPickA=graph.get("is_moving"))
    set_frame = graph.set("current_frame_index", frame)
    side = graph.math("GreaterEqual_IntInt", A=graph.get("facing_direction"), B=2)
    side_branch = graph.place(editor.add_branch_node())
    graph.connect(side, side_branch.find_input_pin("Condition"))
    back_branch = graph.place(editor.add_branch_node())
    graph.connect(graph.math("EqualEqual_IntInt", A=graph.get("facing_direction"), B=1), back_branch.find_input_pin("Condition"))
    graph.flow(entry, set_phase, set_position, set_moving, set_facing, set_elapsed, set_blend, set_frame, side_branch)
    graph.connect(side_branch.find_output_pin("else"), back_branch.find_execute_pin())

    visual = graph.get("player_visual")
    finished_sprite_nodes = []
    for direction, flow_pin in (("side", side_branch.find_output_pin("then")),
                                ("back", back_branch.find_output_pin("then")),
                                ("front", back_branch.find_output_pin("else"))):
        idle_frame = graph.math("SelectInt", A=0, B=graph.get("current_frame_index"), bPickA=graph.get("is_moving"))
        array_item = graph.function("/Script/Engine.KismetArrayLibrary.Array_Get", TargetArray=graph.get(direction + "_frames"), Index=idle_frame)
        last = graph.function("/Script/Paper2D.PaperSpriteComponent.SetSprite", self=visual, NewSprite=array_item.find_output_pin("Item"))
        graph.connect(flow_pin, last.find_execute_pin())
        for index, part_name in enumerate(("walk_head", "walk_body", "walk_left_foot", "walk_right_foot")):
            item = graph.function("/Script/Engine.KismetArrayLibrary.Array_Get", TargetArray=graph.get(direction + "_walk_parts"), Index=index)
            node = graph.function("/Script/Paper2D.PaperSpriteComponent.SetSprite", self=graph.get(part_name), NewSprite=item.find_output_pin("Item"))
            graph.flow(last, node)
            last = node
        finished_sprite_nodes.append(last)

    right = graph.math("EqualEqual_IntInt", A=graph.get("facing_direction"), B=3)
    left = graph.math("EqualEqual_IntInt", A=graph.get("facing_direction"), B=2)
    flip_side = graph.math("BooleanOR",
        A=graph.math("BooleanAND", A=right, B=graph.get("side_sprite_faces_left")),
        B=graph.math("BooleanAND", A=left, B=graph.math("Not_PreBool", A=graph.get("side_sprite_faces_left"))))
    sign = graph.math("SelectFloat", A=-1.0, B=1.0, bPickA=flip_side)
    scale = graph.function(MATH + "BreakVector", InVec=graph.get("visual_scale"))
    scale_vec = graph.math("MakeVector",
        X=graph.math("Multiply_DoubleDouble", A=graph.math("Abs", A=scale.find_output_pin("X")), B=sign),
        Y=scale.find_output_pin("Y"), Z=scale.find_output_pin("Z"))
    scale_set = graph.function("/Script/Engine.SceneComponent.SetRelativeScale3D", self=visual, NewScale3D=scale_vec)
    walking_branch = graph.place(editor.add_branch_node())
    graph.connect(graph.get("is_moving"), walking_branch.find_input_pin("Condition"))
    for node in finished_sprite_nodes:
        graph.flow(node, walking_branch)
    clear_sprite = graph.function("/Script/Paper2D.PaperSpriteComponent.SetSprite", self=visual, NewSprite="None")
    graph.flow(walking_branch.find_output_pin("then"), clear_sprite, scale_set)
    graph.flow(walking_branch.find_output_pin("else"), scale_set)
    # Only the visual moves: collision, camera and interaction origin stay stable.
    wave = graph.math("Sin", A=graph.math("Multiply_DoubleDouble", A=graph.get("walk_phase"), B=12.566370614359172))
    bob = graph.math("Multiply_DoubleDouble", A=graph.math("Multiply_DoubleDouble", A=wave, B=graph.get("walk_bob_cm")), B=graph.get("walk_blend"))
    offset = graph.math("Add_VectorVector", A=graph.get("visual_origin"), B=graph.math("MakeVector", X=0.0, Y=0.0, Z=bob))
    set_location = graph.function("/Script/Engine.SceneComponent.K2_SetRelativeLocation", self=visual, NewLocation=offset, bSweep=False, bTeleport=True)
    target_lean = graph.math("Multiply_DoubleDouble", A=graph.math("Divide_DoubleDouble", A=vx, B=graph.math("FMax", A=graph.get("move_speed"), B=1.0)), B=graph.get("walk_lean_degrees"))
    set_lean = graph.set("visual_lean", graph.math("FInterpTo", Current=graph.get("visual_lean"), Target=target_lean, DeltaTime=dt, InterpSpeed=14.0))
    rotation = graph.math("MakeRotator", Roll=0.0, Pitch=graph.get("visual_lean"), Yaw=0.0)
    set_rotation = graph.function("/Script/Engine.SceneComponent.K2_SetRelativeRotation", self=visual, NewRotation=rotation, bSweep=False, bTeleport=True)
    graph.flow(scale_set, set_location, set_lean, set_rotation)
    # Eight discrete poses made from original texture regions. The large head
    # follows the torso slightly late; short feet alternate lift and contact.
    pose_number = graph.math("Conv_IntToDouble", InInt=graph.get("current_frame_index"))
    angle = graph.math("Multiply_DoubleDouble", A=pose_number, B=0.7853981633974483)
    stride = graph.math("Sin", A=angle)
    reverse_stride = graph.math("Multiply_DoubleDouble", A=stride, B=-1.0)
    double_angle = graph.math("Multiply_DoubleDouble", A=angle, B=2.0)
    body_z = graph.math("Subtract_DoubleDouble", A=graph.math("Multiply_DoubleDouble", A=graph.math("Cos", A=double_angle), B=0.55), B=0.55)
    head_z = graph.math("Subtract_DoubleDouble", A=graph.math("Multiply_DoubleDouble", A=graph.math("Cos", A=graph.math("Subtract_DoubleDouble", A=double_angle, B=0.4)), B=0.35), B=0.35)
    # All player pieces share a ground anchor. Bob, lean, sprite bounds and
    # animation pose must never change whether a prop is in front of the feet.
    # The renderer clamps priorities to signed 16-bit even though this property
    # is int32. One priority unit per cm keeps all current maps safely in range.
    # The static depth compiler uses the exact same -floor(z + .5) rule.
    feet = graph.function(MATH + "BreakVector", InVec=position).find_output_pin("Z")
    depth_bucket = graph.math("FFloor", A=graph.math("Add_DoubleDouble", A=feet, B=0.5))
    # Promotable arithmetic pins begin as wildcards in UE5.8; connect the
    # typed integer output before assigning the other pin's literal default.
    sort = graph.math("Subtract_IntInt", B=depth_bucket, A=0)
    set_sort = graph.function("/Script/Engine.PrimitiveComponent.SetTranslucentSortPriority",
                             self=visual, NewTranslucentSortPriority=sort)
    graph.flow(set_rotation, set_sort)
    tint = graph.property(visual, "/Script/Paper2D.PaperSpriteComponent", "SpriteColor")
    last = set_sort
    for name, order, foot_wave in (("walk_right_foot", 0, reverse_stride), ("walk_left_foot", 1, stride), ("walk_body", 2, None), ("walk_head", 3, None)):
        part = graph.get(name)
        visibility = graph.function("/Script/Engine.SceneComponent.SetVisibility", self=part, bNewVisibility=graph.get("is_moving"), bPropagateToChildren=False)
        # Match the whole idle sprite's current feet depth to avoid a prop
        # cutting between head/body/feet. Tiny Y offsets keep the parts ordered.
        priority = graph.function("/Script/Engine.PrimitiveComponent.SetTranslucentSortPriority", self=part, NewTranslucentSortPriority=sort)
        color = graph.function("/Script/Paper2D.PaperSpriteComponent.SetSpriteColor", self=part, NewColor=tint)
        if foot_wave is not None:
            x = graph.math("Multiply_DoubleDouble", A=foot_wave, B=graph.math("SelectFloat", A=1.25, B=0.25, bPickA=side))
            z = graph.math("Multiply_DoubleDouble", A=graph.math("FMax", A=0.0, B=foot_wave), B=1.25)
        else:
            x = graph.math("Multiply_DoubleDouble", A=stride, B=0.15 if name == "walk_head" else 0.0)
            z = head_z if name == "walk_head" else body_z
        loc = graph.math("MakeVector", X=x, Y=order * 0.01, Z=z)
        placement = graph.function("/Script/Engine.SceneComponent.K2_SetRelativeLocation", self=part, NewLocation=loc, bSweep=False, bTeleport=True)
        graph.flow(last, visibility, priority, color, placement)
        last = placement
    return len(graph.nodes)


def build_event_graph(bp):
    editor = unreal.BlueprintGraphEditor.get_graph_editor(B.find_event_graph(bp))
    graph = Graph(editor)
    tick = B.add_event_override(bp, "ReceiveTick", unreal.IntPoint(-700, 0))
    local = graph.function("/Script/Engine.Pawn.IsLocallyControlled").find_output_pin("ReturnValue")
    branch = graph.place(editor.add_branch_node())
    graph.connect(local, branch.find_input_pin("Condition"))
    graph.flow(tick, branch)
    controller = graph.function("/Script/Engine.GameplayStatics.GetPlayerController", PlayerIndex=0).find_output_pin("ReturnValue")

    def key_pair(first, second):
        values = []
        for key in (first, second):
            node = graph.function("/Script/Engine.PlayerController.GetInputAnalogKeyState", self=controller, Key=key)
            values.append(node.find_output_pin("ReturnValue"))
        return graph.math("FMax", A=values[0], B=values[1])

    horizontal = graph.math("Subtract_DoubleDouble", A=key_pair("D", "Right"), B=key_pair("A", "Left"))
    vertical = graph.math("Subtract_DoubleDouble", A=key_pair("W", "Up"), B=key_pair("S", "Down"))
    direction = graph.math("Normal", A=graph.math("MakeVector", X=horizontal, Y=0.0, Z=vertical))
    move = graph.function("/Script/Engine.Pawn.AddMovementInput", WorldDirection=direction, ScaleValue=1.0, bForce=False)
    speed = graph.place(editor.add_set_member_variable_node("MaxSpeed", "/Script/Engine.FloatingPawnMovement"))
    graph.connect(graph.get("movement_component"), speed.find_input_pin("self"))
    graph.connect(graph.math("FMax", A=graph.get("move_speed"), B=0.0), speed.find_input_pin("MaxSpeed"))
    refresh = graph.function(bp.generated_class().get_path_name() + ".RefreshPlayerVisual")
    graph.flow(branch.find_output_pin("then"), speed, move, refresh)

    begin = B.add_event_override(bp, "ReceiveBeginPlay", unreal.IntPoint(-700, 1700))
    updated = graph.function("/Script/Engine.MovementComponent.SetUpdatedComponent",
                             self=graph.get("movement_component"), NewUpdatedComponent=graph.get("collision_component"))
    location = graph.function("/Script/Engine.Actor.K2_GetActorLocation").find_output_pin("ReturnValue")
    origin = graph.function("/Script/Engine.MovementComponent.SetPlaneConstraintOrigin",
                            self=graph.get("movement_component"), PlaneOrigin=location)
    initial_position = graph.set("previous_position", location)
    relative = graph.property(graph.get("player_visual"), "/Script/Engine.SceneComponent", "RelativeLocation")
    visual_origin = graph.set("visual_origin", relative)
    initial_refresh = graph.function(bp.generated_class().get_path_name() + ".RefreshPlayerVisual")
    graph.flow(begin, updated, origin, initial_position, visual_origin, initial_refresh)
    return len(graph.nodes) + 2


def ensure_player_blueprint():
    require_unreal_project()
    bp = ASSETS.load_asset(PLAYER_PATH) if ASSETS.does_asset_exist(PLAYER_PATH) else None
    if bp is not None and ASSETS.get_metadata_tag(bp, "PlayerGraphBuilderVersion") == VERSION:
        compile_checked(bp)
        return bp
    if bp is None:
        factory = unreal.BlueprintFactory()
        factory.set_editor_property("parent_class", unreal.Pawn)
        bp = TOOLS.create_asset(PLAYER_PATH.rsplit("/", 1)[1], PLAYER_PATH.rsplit("/", 1)[0], unreal.Blueprint, factory)
        if bp is None:
            raise RuntimeError("Cannot create player Blueprint")
    if B.get_blueprint_parent_class(bp) != unreal.Pawn.static_class():
        raise RuntimeError("Player Blueprint must derive directly from native Engine.Pawn")
    if not ASSETS.get_metadata_tag(bp, "PlayerComponentsCreated"):
        create_components(bp)
        ASSETS.set_metadata_tag(bp, "PlayerComponentsCreated", "1")
    ensure_walk_components(bp)
    editor = unreal.BlueprintGraphEditor.get_graph_editor(B.find_event_graph(bp))
    editor.remove_nodes(editor.list_all_nodes())
    for existing_graph in B.list_graphs(bp):
        if existing_graph.get_name().startswith("RefreshPlayerVisual"):
            B.remove_function_graph(bp, existing_graph.get_name())
    add_variables(bp, editor)
    compile_checked(bp)
    # Declare the callable signature first. UE5.8 operator node spawners can
    # retain an invalid template when re-used across an intermediate compile.
    stub = unreal.BlueprintGraphEditor.create_and_edit_function_graph(bp, "RefreshPlayerVisual")
    stub.set_function_is_public()
    compile_checked(bp)
    event_nodes = build_event_graph(bp)
    refresh_nodes = build_refresh_graph(bp)
    compile_checked(bp)
    defaults = unreal.get_default_object(bp.generated_class())
    defaults.set_editor_property("auto_possess_player", unreal.AutoReceiveInput.PLAYER0)
    defaults.set_editor_property("auto_possess_ai", unreal.AutoPossessAI.DISABLED)
    for rotation in ("pitch", "yaw", "roll"):
        defaults.set_editor_property("use_controller_rotation_" + rotation, False)
    ASSETS.set_metadata_tag(bp, "PlayerGraphBuilderVersion", VERSION)
    ASSETS.set_metadata_tag(bp, "PlayerRuntimeImplementation", "Blueprint + native Engine/Paper2D components; no project native module")
    if not ASSETS.save_loaded_asset(bp, only_if_is_dirty=False):
        raise RuntimeError("Cannot save player Blueprint")
    QA_ROOT.mkdir(parents=True, exist_ok=True)
    report = {"success": True, "blueprint": PLAYER_PATH, "parent": "/Script/Engine.Pawn",
              "version": VERSION, "refresh_nodes": refresh_nodes, "event_nodes": event_nodes,
              "movement": "FloatingPawnMovement, normalized XZ input, Y plane constraint",
              "input": "WASD and arrow keys", "speed_cm_per_second": 400,
              "idle_frame_seconds": 0.3, "walking_poses": 8,
              "walk_cycle_distance_cm": 160, "walk_art": "Original texture regions: head, body, two feet",
              "diagonal_speed_normalized": True, "diagonal_facing_hysteresis": 0.15,
              "facing_basis": "nonzero pending/last-consumed movement input; actual velocity still controls walking and idle",
              "translucency_sort": "-floor(root_world_Z_cm + 0.5), signed16-bit safe; same priority for idle and all walk parts",
              "facing_values": {"front": 0, "back": 1, "left": 2, "right": 3},
              "runtime_python": False, "project_native_module": False}
    (QA_ROOT / "player_blueprint_build.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    return bp


if __name__ == "__main__":
    try:
        ensure_player_blueprint()
    except Exception:
        import traceback
        QA_ROOT.mkdir(parents=True, exist_ok=True)
        (QA_ROOT / "player_blueprint_error.txt").write_text(traceback.format_exc(), encoding="utf-8")
        raise
