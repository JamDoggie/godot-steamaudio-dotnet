extends EditorNode3DGizmoPlugin
class_name EventEmitterGizmoPlugin

var speaker_scene : PackedScene = preload("res://addons/steam-audio-cs-plugin/fmod-gizmos/speaker.glb")
var speaker_mesh : Mesh = null
var speaker_mat : StandardMaterial3D = null
var cone_mat : StandardMaterial3D = null

func _has_gizmo(node):
	return node is FmodEventEmitter3D

func _get_gizmo_name():
	return "FmodEventSpeaker"

func _init():
	var meshNode : Node3D = speaker_scene.instantiate(PackedScene.GEN_EDIT_STATE_DISABLED)
	
	speaker_mat = preload("res://addons/steam-audio-cs-plugin/fmod-gizmos/speaker-mat.tres") as StandardMaterial3D
	cone_mat = preload("res://addons/steam-audio-cs-plugin/fmod-gizmos/speaker-cone.tres") as StandardMaterial3D
	
	print(preload("res://addons/steam-audio-cs-plugin/fmod-gizmos/speaker-mat.tres"))
	
	if (meshNode.get_node('speaker-mesh') != null 
		and meshNode.get_node('speaker-mesh') is MeshInstance3D):
		speaker_mesh = (meshNode.get_node('speaker-mesh') as MeshInstance3D).mesh

func _redraw(gizmo):
	gizmo.clear()
	var n = gizmo.get_node_3d()
	
	if (speaker_mat == null):
		print("speaker mat is null")
	
	if speaker_mesh != null and speaker_mat != null and cone_mat != null:
		speaker_mesh.surface_set_material(0, speaker_mat)
		speaker_mesh.surface_set_material(1, cone_mat)
		speaker_mesh.surface_set_material(2, speaker_mat)
		gizmo.add_mesh(speaker_mesh)
