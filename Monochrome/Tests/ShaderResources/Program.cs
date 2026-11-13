using Monochrome.CoreTests;
var tests = new Action[] { ShaderResourceChecks.ReusesClone, ShaderResourceChecks.ReplacesClone,
    ShaderResourceChecks.FailedCloneCanRetry, ShaderResourceChecks.BorrowedSurvivesUnload, ShaderResourceChecks.UnloadIsIdempotent };
foreach (var test in tests) { test(); Console.WriteLine("PASS " + test.Method.Name); }
Console.WriteLine($"{tests.Length}/{tests.Length} shader resource ownership checks passed.");
