//using StarDebuCat.Algorithm;

//namespace MilkWangBase.Utility;

//public class AutoCurve
//{
//    int sampleRate = 32;
//    int currentLoop;
//    float currentValue;
//    public void Set(float value)
//    {
//        currentValue = value;
//        currentLoop++;
//        if (currentLoop % sampleRate == 0)
//        {
//            Curve.AddPoint(currentLoop, currentValue);
//        }
//    }
//    public float SampleFuture(float time)
//    {
//        return Curve.Sample(currentLoop + time);
//    }
//    public static implicit operator float(AutoCurve curve)
//    {
//        return curve.currentValue;
//    }
//    Curve Curve = new();
//}
