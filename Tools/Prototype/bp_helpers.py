"""Editor-only helpers for producing saved, portable UE5.8 Blueprint graphs."""
from pathlib import Path
import unreal

ROOT = Path(__file__).resolve().parents[2]
PREFIX = '/Game/Jonggu/Prototype'
B = unreal.BlueprintEditorLibrary
ASSETS = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
MATH = '/Script/Engine.KismetMathLibrary.'

def class_path(cls):
    if isinstance(cls, str):
        return cls
    if hasattr(cls, 'generated_class'):
        cls = cls.generated_class()
    return cls.get_path_name() if hasattr(cls, 'get_path_name') else cls.static_class().get_path_name()

def literal(value):
    if isinstance(value, bool): return str(value).lower()
    if value is None: return 'None'
    if hasattr(value, 'get_path_name'): return value.get_path_name()
    return str(value)

def pin_type(kind):
    if not isinstance(kind, str): return kind
    if kind == 'float': kind = 'real'
    if kind == 'vector': return B.get_struct_type(unreal.load_object(None, '/Script/CoreUObject.Vector'))
    if kind.startswith('object:'): return B.get_object_reference_type(unreal.load_class(None, kind[7:]))
    if kind.startswith('array:'): return B.get_array_type(pin_type(kind[6:]))
    return B.get_basic_type_by_name(kind)

def compile_bp(bp):
    if not B.compile_blueprint(bp):
        errors = []
        for graph in B.list_graphs(bp):
            ed = unreal.BlueprintGraphEditor.get_graph_editor(graph)
            errors.extend(str(n) for n in ed.list_nodes_with_errors())
        raise RuntimeError('Blueprint compile failed: ' + bp.get_name() + '\n' + '\n'.join(errors))
    return bp

def ensure_bp(name, parent):
    asset_path = PREFIX + '/' + name
    bp = ASSETS.load_asset(asset_path) if ASSETS.does_asset_exist(asset_path) else None
    if bp is None:
        factory = unreal.BlueprintFactory()
        factory.set_editor_property('parent_class', parent)
        bp = unreal.AssetToolsHelpers.get_asset_tools().create_asset(name, PREFIX, unreal.Blueprint, factory)
    if bp is None: raise RuntimeError('Cannot create ' + asset_path)
    # These assets belong only to this generator; original migration assets are never rebuilt.
    for graph in list(B.list_graphs(bp)):
        if graph.get_name() not in ('EventGraph', 'UserConstructionScript'):
            B.remove_function_graph(bp, graph.get_name())
    event = unreal.BlueprintGraphEditor.get_graph_editor(B.find_event_graph(bp))
    event.remove_nodes(event.list_all_nodes())
    return bp

def add_vars(bp, specs):
    ed = unreal.BlueprintGraphEditor.get_graph_editor(B.find_event_graph(bp))
    for name, kind, value in specs:
        if not ed.add_member_variable(name, pin_type(kind), literal(value)):
            # Existing variables survive an idempotent rebuild; defaults are applied after compile.
            pass
        B.set_blueprint_variable_instance_editable(bp, name, True)
    compile_bp(bp)
    cdo = unreal.get_default_object(bp.generated_class())
    for name, kind, value in specs:
        if kind in ('bool', 'int', 'real', 'float', 'string'):
            cdo.set_editor_property(name, value)
    return bp

def declare_function(bp, name, inputs=None, outputs=None, pure=False):
    ed = unreal.BlueprintGraphEditor.create_and_edit_function_graph(bp, name)
    ed.set_function_is_public()
    ed.set_is_pure_function(pure)
    for key, kind in (inputs or {}).items():
        ed.add_graph_input_parameter(key, pin_type(kind))
    for key, kind in (outputs or {}).items():
        ed.add_graph_output_parameter(key, pin_type(kind))
    return ed

def save_bp(bp):
    compile_bp(bp)
    ASSETS.set_metadata_tag(bp, 'JongguPrototypeBuilder', '1')
    if not ASSETS.save_loaded_asset(bp, only_if_is_dirty=False):
        raise RuntimeError('Cannot save ' + bp.get_path_name())
    return bp

class G:
    def __init__(graph, bp, name=None, editor=None, entry=None):
        graph.bp = bp
        graph.editor = editor or unreal.BlueprintGraphEditor.get_graph_editor_by_name(bp, name)
        graph.nodes = []
        graph.entry = entry or graph.editor.find_graph_entry_pin()
        graph.tails = [graph.entry] if graph.entry.is_valid() else []

    @classmethod
    def event(cls, bp, name):
        ed = unreal.BlueprintGraphEditor.get_graph_editor(B.find_event_graph(bp))
        node = B.add_event_override(bp, name, unreal.IntPoint(-400, len(ed.list_all_nodes()) * 70))
        return cls(bp, editor=ed, entry=node.find_then_pin())

    def place(graph, node):
        if node is None: raise RuntimeError('Node creation failed in ' + graph.bp.get_name())
        i = len(graph.editor.list_all_nodes())
        node.set_node_pos(unreal.IntPoint((i % 8) * 310, (i // 8) * 200))
        graph.nodes.append(node)
        return node

    @staticmethod
    def wire(value, pin):
        if not pin.is_valid(): raise RuntimeError('Invalid target pin')
        if isinstance(value, unreal.BlueprintGraphPin):
            if not value.is_valid() or not value.try_create_connection(pin):
                raise RuntimeError('Cannot connect ' + str(value) + ' -> ' + str(pin))
        elif not pin.set_pin_value(literal(value)):
            raise RuntimeError('Cannot set ' + str(pin) + ' = ' + literal(value))

    def param(graph, name):
        return graph.entry.get_owning_node().find_output_pin(name)

    def fn(graph, path, **inputs):
        node = graph.place(graph.editor.add_call_function_node(path))
        # Specialize wildcard/dynamic output pins before writing primitive defaults.
        for key, value in sorted(inputs.items(), key=lambda item: not isinstance(item[1], unreal.BlueprintGraphPin)):
            graph.wire(value, node.find_input_pin(key))
        return node

    def emit(graph, node):
        for tail in graph.tails: graph.wire(tail, node.find_execute_pin())
        then = node.find_then_pin()
        graph.tails = [then] if then.is_valid() else []
        return node

    def call(graph, path, **inputs): return graph.emit(graph.fn(path, **inputs))
    def pure(graph, path, **inputs): return graph.fn(path, **inputs).find_output_pin('ReturnValue')
    def math(graph, name, **inputs): return graph.pure(MATH + name, **inputs)

    def get(graph, name, obj=None, cls=None):
        node = graph.place(graph.editor.add_get_member_variable_node(name, class_path(cls) if cls else ''))
        if obj is not None: graph.wire(obj, node.find_input_pin('self'))
        return node.find_output_pin(name)

    def set(graph, name, value, obj=None, cls=None):
        node = graph.place(graph.editor.add_set_member_variable_node(name, class_path(cls) if cls else ''))
        if obj is not None: graph.wire(obj, node.find_input_pin('self'))
        graph.wire(value, node.find_input_pin(name))
        graph.emit(node)
        return node

    def local(graph, name):
        return graph.place(graph.editor.add_get_local_variable_node(name)).find_output_pin(name)

    def set_local(graph, name, value):
        node = graph.place(graph.editor.add_set_local_variable_node(name))
        graph.wire(value, node.find_input_pin(name))
        return graph.emit(node)

    def when(graph, condition, yes, no=None):
        node = graph.place(graph.editor.add_branch_node())
        graph.wire(condition, node.find_input_pin('Condition'))
        graph.emit(node)
        graph.tails = [node.find_output_pin('then')]
        yes(graph)
        yes_tails = graph.tails
        graph.tails = [node.find_output_pin('else')]
        if no: no(graph)
        graph.tails = yes_tails + graph.tails

    def guard(graph, condition):
        node = graph.place(graph.editor.add_branch_node())
        graph.wire(condition, node.find_input_pin('Condition'))
        graph.emit(node)
        graph.tails = [node.find_output_pin('then')]

    def ret(graph, **outputs):
        node = graph.place(graph.editor.add_return_node())
        for key, value in outputs.items(): graph.wire(value, node.find_input_pin(key))
        graph.emit(node)
        graph.tails = []

    def join(graph, *parts):
        value = parts[0] if parts else ''
        for part in parts[1:]:
            value = graph.pure('/Script/Engine.KismetStringLibrary.Concat_StrStr', A=value, B=part)
        return value

    def integer_string(graph, value):
        return graph.pure('/Script/Engine.KismetStringLibrary.Conv_IntToString', InInt=value)

    def cast(graph, obj, cls, pure=True):
        target = unreal.load_class(None, class_path(cls)) if isinstance(cls, str) else (cls.generated_class() if hasattr(cls, 'generated_class') else cls)
        name = target.get_name()
        base = unreal.SaveGame if 'Save' in name else unreal.GameInstance if 'Session' in name else unreal.HUD if 'HUD' in name else unreal.Actor
        # Newly generated BP classes need not have an ActionDatabase entry yet.
        # Create a native cast and retarget the baked class through UE's explicit API.
        action = 'Utilities|Casting|CastTo' + base.static_class().get_name()
        node = graph.place(graph.editor.create_node_from_name(action, unreal.Vector2D(0,0), []))
        if not graph.editor.retarget_node_class(node, base.static_class(), target):
            raise RuntimeError('Cannot retarget cast to ' + name)
        graph.wire(obj, node.find_input_pin('Object'))
        graph.emit(node)
        for pin in node.list_output_pins():
            if str(pin.get_pin_name()).startswith('As'):
                return pin
        raise RuntimeError('Cast output missing')
