using Palsoul.Core;

var parameters = new CaptureParameters(.5f, .5f, 2, 1.5f, 1.4f, 1.2f, 1.15f, 1.4f, .1f);
int passed = 0;
void Check(string name, float actual, float expected)
{
    if (Math.Abs(actual - expected) > .0001f) throw new Exception($"{name}: expected {expected}, got {actual}");
    passed++;
    Console.WriteLine($"PASS {name}: {actual:F4}");
}
Check("healthy", CaptureRules.Calculate(.3f, 1, false, CaptureStatus.None, parameters), .15f);
Check("threshold", CaptureRules.Calculate(.3f, .5f, false, CaptureStatus.None, parameters), .15f);
Check("critical", CaptureRules.Calculate(.3f, .2f, false, CaptureStatus.None, parameters), .42f);
Check("zero HP", CaptureRules.Calculate(.3f, 0, false, CaptureStatus.None, parameters), .6f);
Check("stealth", CaptureRules.Calculate(.3f, .2f, true, CaptureStatus.None, parameters), .63f);
Check("stunned", CaptureRules.Calculate(.3f, .2f, true, CaptureStatus.Stunned, parameters), .882f);
Check("burning", CaptureRules.Calculate(.3f, 1, false, CaptureStatus.Burning, parameters), .18f);
Check("frozen", CaptureRules.Calculate(.3f, 1, false, CaptureStatus.Frozen, parameters), .21f);
Check("poisoned", CaptureRules.Calculate(.3f, 1, false, CaptureStatus.Poisoned, parameters), .1725f);
Check("clamp", CaptureRules.Calculate(.3f, 0, true, CaptureStatus.Stunned | CaptureStatus.Burning, parameters), 1);
Check("higher tier", CaptureRules.Calculate(.6f, .2f, false, CaptureStatus.None, parameters), .84f);
Check("rare healthy", CaptureRules.Calculate(.3f, 1, false, CaptureStatus.None, parameters, true, .2f), .015f);
Check("rare critical", CaptureRules.Calculate(.3f, .2f, false, CaptureStatus.None, parameters, true, .2f), .42f);
Check("HP below zero", CaptureRules.Calculate(.3f, -1, false, CaptureStatus.None, parameters), .6f);
Check("HP above max", CaptureRules.Calculate(.3f, 2, false, CaptureStatus.None, parameters), .15f);
Console.WriteLine($"{passed} checks passed against production CaptureRules.cs.");
