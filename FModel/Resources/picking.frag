#version 330

uniform uint uA;
uniform uint uB;
uniform uint uC;
uniform uint uD;

out uvec4 FragColor;

void main()
{
    FragColor = uvec4(uA, uB, uC, uD);
}
