shader_type spatial;

render_mode cull_disabled;

uniform float discard_min : hint_range(0.0, 1.1) = 0.3;
uniform float vfx_alpha  : hint_range(0.0, 1.0) = 0.3;
uniform float length_shown : hint_range(0.0, 1.0) = 1.0;
uniform vec2 scroll_velocity = vec2(1.0);
uniform sampler2D noise_tex : hint_default_white, filter_linear_mipmap, repeat_enable;

void fragment() {
	if (discard_min > 1.0) discard;
	if (UV.y > length_shown) discard;
	vec2 uv_off = TIME * scroll_velocity;
	ALBEDO = texture(noise_tex, UV + uv_off).rgb;
	ALPHA = vfx_alpha;
	if (length(ALBEDO) < length(vec3(discard_min)))
	{
		discard;
	}
}
