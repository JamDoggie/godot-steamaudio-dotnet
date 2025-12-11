extends EditorNode3DGizmoPlugin
class_name SteamAudioBakeGizmo

func create_custom_mat(color : Color):
	var mat : StandardMaterial3D = StandardMaterial3D.new()
	
	mat.albedo_color = color
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.no_depth_test = true
	mat.render_priority = StandardMaterial3D.RENDER_PRIORITY_MIN + 1
	mat.set_flag(BaseMaterial3D.FLAG_DISABLE_FOG, true)
	mat.set_flag(BaseMaterial3D.FLAG_ALBEDO_FROM_VERTEX_COLOR, true)
	
	return mat

func _init():
	add_material("main", create_custom_mat(Color(1.0, 0.1, 0.1)))
	
	var probe_mat : StandardMaterial3D = create_custom_mat(Color(0.3, 0.3, 1.0, 0.2))
	
	add_material("probe", probe_mat)
	create_handle_material("handles")

func _has_gizmo(node):
	return node is SteamAudioBaker
	
func _get_gizmo_name():
	return "SteamAudioBaker"

func _get_handle_name(gizmo, handle_id, secondary):
	match handle_id:
		0: return "x"
		1: return "y"
		2: return "z"
		
func _get_handle_value(gizmo, handle_id, secondary):
	var baker : SteamAudioBaker = gizmo.get_node_3d()
	match handle_id:
		0: return baker.BakedAreaExtents.x
		1: return baker.BakedAreaExtents.y
		2: return baker.BakedAreaExtents.z

func _redraw(gizmo):
	gizmo.clear()
	
	if RenderingServer.get_current_rendering_method() == "mobile":
		return
	
	var audioBaker : SteamAudioBaker = gizmo.get_node_3d()
	
	var baker_found : bool = false
	
	for node in EditorInterface.get_selection().get_selected_nodes():
		if node == audioBaker:
			baker_found = true
			break
	
	if not baker_found:
		return
	
	var bakedAreaExtents : Vector3 = audioBaker.BakedAreaExtents
	
	var rotInverse : Basis = audioBaker.basis.inverse()
	
	var vertices = [
		Vector3(bakedAreaExtents.x, bakedAreaExtents.y, bakedAreaExtents.z),
		Vector3(-bakedAreaExtents.x, bakedAreaExtents.y, bakedAreaExtents.z),
		Vector3(-bakedAreaExtents.x, -bakedAreaExtents.y, bakedAreaExtents.z),
		Vector3(bakedAreaExtents.x, -bakedAreaExtents.y, bakedAreaExtents.z),
		Vector3(bakedAreaExtents.x, bakedAreaExtents.y, -bakedAreaExtents.z),
		Vector3(-bakedAreaExtents.x, bakedAreaExtents.y, -bakedAreaExtents.z),
		Vector3(-bakedAreaExtents.x, -bakedAreaExtents.y, -bakedAreaExtents.z),
		Vector3(bakedAreaExtents.x, -bakedAreaExtents.y, -bakedAreaExtents.z)
	]
	
	for i in range(len(vertices)):
		vertices[i] = rotInverse * vertices[i]
	
	var cube_lines : PackedVector3Array = [
		vertices[0], vertices[1],
		vertices[1], vertices[2],
		vertices[2], vertices[3],
		vertices[3], vertices[0],
		vertices[4], vertices[5],
		vertices[5], vertices[6],
		vertices[6], vertices[7],
		vertices[7], vertices[4],
		vertices[0], vertices[4],
		vertices[1], vertices[5],
		vertices[2], vertices[6],
		vertices[3], vertices[7]
	]
	
	gizmo.add_lines(cube_lines, get_material("main", gizmo), false)
	
	var handles = PackedVector3Array()
	handles.push_back(rotInverse * Vector3(bakedAreaExtents.x, 0, 0)) # x-handle, handle_id 0
	handles.push_back(rotInverse * Vector3(0, bakedAreaExtents.y, 0)) # y-handle, handle_id 1
	handles.push_back(rotInverse *  Vector3(0, 0, bakedAreaExtents.z)) # z-handle, handle_id 2
	gizmo.add_handles(handles, get_material("handles", gizmo), [])
	
	# Draw probes
	var probe_mat : Material = get_material("probe", gizmo)
	
	if audioBaker.ProbeData != null and len(audioBaker.ProbeData) > 0 and len(audioBaker.ProbeData) % 4 == 0:
		for i in range(len(audioBaker.ProbeData) / 4):
			var probeLoc : Vector3 = Vector3(
				audioBaker.ProbeData[(i * 4) + 0] , 
				audioBaker.ProbeData[(i * 4) + 1], 
				audioBaker.ProbeData[(i * 4) + 2]
			)
			
			var radius : float = audioBaker.ProbeData[(i * 4) + 3]
			
			var sphereMesh : SphereMesh = SphereMesh.new()
			sphereMesh.radius = (radius) / 3
			sphereMesh.height = (radius * 2) / 3
			sphereMesh.radial_segments = 8
			sphereMesh.rings = 8
			sphereMesh.surface_set_material(0, probe_mat)
			
			var probeTransform : Transform3D = Transform3D.IDENTITY
			probeTransform.origin = probeLoc - audioBaker.global_position
			
			gizmo.add_mesh(sphereMesh, probe_mat, probeTransform)
	
func _set_handle(gizmo: EditorNode3DGizmo, handle_id: int, secondary: bool, camera: Camera3D, screen_pos: Vector2):
	var baker : SteamAudioBaker = gizmo.get_node_3d()
	
	var plane : Plane;
	match handle_id:
		0: plane = Plane.PLANE_XY
		1: plane = Plane.PLANE_XY
		2: plane = Plane.PLANE_YZ
	plane = baker.global_transform * plane
	
	var ray_from = camera.project_ray_origin(screen_pos)
	var ray_to = camera.project_ray_normal(screen_pos)
	var drag_to = ray_from + ray_to * baker.global_transform.origin.distance_to(ray_from)

	match handle_id:
		0: baker.BakedAreaExtents.x = drag_to.x
		1: baker.BakedAreaExtents.y = drag_to.y
		2: baker.BakedAreaExtents.z = drag_to.z
	baker.update_gizmos()
