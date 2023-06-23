shader_type spatial;
// includes point rotation functions
#include "res://Assets/Materials/ShaderIncludes/point_rotations.gdshaderinc"


render_mode unshaded, cull_disabled, shadows_disabled;

uniform sampler2D potion_gradient : hint_default_white;
uniform vec4 fresnel_colour : source_color = vec4(1.0);
uniform vec4 edge_colour : source_color = vec4(1.0);

uniform float fill : hint_range(0.0, 1.0) = 0.75;
uniform float edge_thickness : hint_range(0.0, 1.0) = 0.02;
uniform float mesh_radius = 0.2;
instance uniform vec2 potion_wobble = vec2(0.0);

varying vec3 LocalPosition;

void vertex()
{
	LocalPosition = VERTEX;
	LocalPosition = rotate_point_x(potion_wobble.x, LocalPosition);
	LocalPosition = rotate_point_z(potion_wobble.y, LocalPosition);
}

void fragment() {
	
	
	float y_pos = (LocalPosition.y/mesh_radius) * 0.5 + 0.5;
	if (y_pos > fill) {
		discard;
	}

	if (fill < 1.0 && abs(y_pos - fill) < edge_thickness) {
		ALBEDO = edge_colour.rgb;
	} else {	
		if (FRONT_FACING) {
				ALBEDO = texture(potion_gradient, vec2(y_pos, 0.0)).rgb;
				float fresnel = 1.0 - dot(NORMAL, VIEW);
				ALBEDO = mix(ALBEDO, fresnel_colour.rgb, pow(fresnel, 2.0));
		} else {
			ALBEDO = texture(potion_gradient, vec2(1.0, 0.0)).rgb;
		}
	}
}
