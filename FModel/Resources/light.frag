#version 330

uniform sampler2D uIcon;
uniform vec4 uColor;

in vec2 fTexCoords;

out vec4 FragColor;

void main()
{
    vec4 color = uColor * texture(uIcon, fTexCoords);
    if (color.a < 0.1) discard;

    FragColor = uColor;
}
