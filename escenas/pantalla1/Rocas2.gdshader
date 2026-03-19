shader_type canvas_item;

uniform sampler2D tex : repeat_disable;

void fragment() {
    COLOR = texture(tex, UV);
}