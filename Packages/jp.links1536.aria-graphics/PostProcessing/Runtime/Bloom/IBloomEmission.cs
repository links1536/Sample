namespace Aria.Rendering.Universal.PostProcessing.Bloom
{
	public interface IBloomEmission
	{
		float Threshold { get; }
		float Intensity { get; }
		int Clamp { get; }
	}
}
