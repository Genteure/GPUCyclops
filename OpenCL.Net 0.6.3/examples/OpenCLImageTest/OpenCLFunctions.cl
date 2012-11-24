kernel void FilterImage( float inputLeft,
                         float inputTop,
                         float inputWidth,
                         float inputHeight,
                         float outputLeft,
                         float outputTop,
                         float outputWidth,
                         float outputHeight,
                         read_only image2d_t input,
                         write_only image2d_t output,
                         sampler_t sampler )
{
	size_t x = get_global_id(0);
	size_t y = get_global_id(1);
	int width = get_global_size(0);
	int height = get_global_size(1);
	
	float nX = x/(float)width;
	float nY = y/(float)height;
	float inputX = inputLeft+inputWidth*nX;
	float inputY = inputTop+inputHeight*nY;
	float outputX = outputLeft+outputWidth*x;
	float outputY = outputTop+outputHeight*y;
	uint4 rgba = read_imageui(input, sampler, (float2)(inputX,inputY));
	write_imageui(output,convert_int2((float2)(outputX,outputY)),rgba);
}
