namespace Aria.Rendering.Universal.PostProcessing
{
	interface IAnimeDiffusion
	{
		bool Active { get; }
		float SampleScale { get; }
		float Weight { get; }
	}
}
