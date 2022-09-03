#version 330

layout (location = 0) out vec4 Out_Color;

in vec2 Frag_UV;
in vec4 Frag_Color;

uniform sampler2D Texture;

void main()
{
    Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
}
