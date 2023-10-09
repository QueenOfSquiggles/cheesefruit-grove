
vec3 rotate_point_x(float theta, vec3 point) {
	mat3 rot_mat = mat3(
		vec3(1.0, 0.0, 0.0), 
		vec3(0.0, cos(theta), -sin(theta)), 
		vec3(0.0, sin(theta), cos(theta))
	);
	return point * rot_mat;
}
vec3 rotate_point_y(float theta, vec3 point) {
	mat3 rot_mat = mat3(
		vec3(cos(theta), 0.0, sin(theta)), 
		vec3(0.0, 1.0, 0.0), 
		vec3(-sin(theta), 0.0, cos(theta))
	);
	return point * rot_mat;
}
vec3 rotate_point_z(float theta, vec3 point) {
	mat3 rot_mat = mat3(
		vec3(cos(theta), -sin(theta), 0.0), 
		vec3(sin(theta), cos(theta), 0.0), 
		vec3(0.0, 0.0, 1.0)
	);
	return point * rot_mat;
}