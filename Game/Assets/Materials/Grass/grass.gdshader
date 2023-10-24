shader_type spatial;
render_mode cull_disabled;

#include "res://Assets/Materials/ShaderIncludes/grass_shader_help.gdshaderinc"

group_uniforms colouration;
	uniform vec4 grass_colour_base : source_color = vec4(0.0, 0.0, 0.0, 1.0);
	uniform vec4 grass_colour_tip : source_color = vec4(0.0, 1.0, 0.0, 1.0);
group_uniforms wind;
	uniform float grass_sway_pitch = 1.0;
	uniform float grass_sway_yaw = 1.0;
	uniform sampler2D wind_noise : filter_linear_mipmap, hint_default_white;
	uniform float wind_noise_scale = 0.5;
	uniform float wind_scroll = 0.1;
group_uniforms PBR;
	uniform float specular_amount : hint_range(0.0, 1.0) = 0.3;
	uniform float min_roughness = 0.3;
	uniform float max_roughness = 1.0;
	uniform float tip_highlight_threshold = 0.8;
	uniform float tip_height_curve_power = 3.0;
	uniform float tip_light_curve_power = 2.0;
group_uniforms PlayerAffect;
	uniform float player_radius = 0.5;
	uniform float player_affect = 0.2;
	
global uniform vec3 weather_wind_velocity;
global uniform vec3 player_position;
global uniform float weather_wetness;

const vec3 UP = vec3(0.0, 1.0, 0.0);
const float SMALLINATOR = 0.001;

varying float wind_strength;
varying float pbr_rough;
varying float pbr_spec;
varying float tip_highlight;

void vertex() {
	NORMAL = UP;
	COLOR = mix(grass_colour_base, grass_colour_tip, 1.0 - UV.y);
	vec3 worldPos_static = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 worldPos = worldPos_static * wind_noise_scale;
	float time = TIME * length(weather_wind_velocity) * wind_scroll;
	vec3 wind_dir = normalize(weather_wind_velocity);
	worldPos += wind_dir * time;
	mat3 to_model = inverse(mat3(MODEL_MATRIX));
	vec3 wind_forward = to_model * wind_dir;
	vec3 wind_right = normalize(cross(wind_forward, UP));
	float wind_noise_sample = texture(wind_noise, worldPos.xz + 0.1).r;
	wind_strength = length(weather_wind_velocity) * pow(wind_noise_sample, 1.5);
	
	float sway_pitch = grass_sway_pitch * wind_strength;
	float sway_yaw = grass_sway_yaw * wind_strength * sin(time);
	
	if (distance(worldPos_static.xz, player_position.xz) < player_radius) {
		float factor = length(worldPos_static.xz - player_position.xz) / player_radius;
		//COLOR = vec4(1.0, 0.0, 0.0, 1.0);
		wind_strength = 0.0;
		vec2 dir = ((player_position.xz - worldPos_static.xz) / player_radius) * player_affect;
		sway_pitch = mix(dir.y, sway_pitch, factor);
		sway_yaw = mix(dir.x, sway_yaw, factor);
	}
	mat3 rot_right = mat3_from_axis_angle(sway_pitch, wind_right);
	mat3 rot_forward = mat3_from_axis_angle(sway_yaw, wind_forward);
	vec3 sway_vec = rot_right * rot_forward * VERTEX;
	vec3 sway_amount = (rot_right * rot_forward * vec3(1.0));
	VERTEX = sway_vec;
		/// ROUGHNESS
	float wetc = clamp(weather_wetness, 0.0, 1.0);
	float rmin = max(min_roughness, wetc);
	float rmax = min(max_roughness, 1.0 - wetc);
	float sway_factor = (length(sway_vec) * wind_strength);
	
	float tip_y = smoothstep(tip_highlight_threshold, 1.0, (1.0 - UV.y) * tip_height_curve_power);
	float sway_light = smoothstep(0.0, 1.0, sway_factor) * tip_y;
	tip_highlight = pow(sway_light, tip_light_curve_power);
	pbr_spec = tip_highlight;
	pbr_rough = mix(clamp(1.0 - (sway_factor * wetc * 2.0), rmin, rmax), rmin, tip_highlight);
}

void fragment() {
	NORMAL = FRONT_FACING ? NORMAL : -NORMAL;
	ALBEDO = mix(COLOR.rgb, vec3(1.0), tip_highlight);
	SPECULAR = pbr_spec;
	ROUGHNESS = pbr_rough;
}
