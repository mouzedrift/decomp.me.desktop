shader_type canvas_item;

uniform float speed = -30.0; // pixels per second

void fragment() {
    vec2 offset = vec2(TIME * speed, 0.0);
    vec2 moving_uv = UV + offset / vec2(textureSize(TEXTURE, 0));
    COLOR = texture(TEXTURE, moving_uv);
}
