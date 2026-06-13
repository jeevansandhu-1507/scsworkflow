using System;
using System.Linq;
using System.Collections.Generic;

var lines = System.IO.File.ReadAllLines("Models/GlMappingService.cs");
var expectedGLs = new List<int> {
    5920, 5111, 5141, 5522, 5543, 5618,
    5925, 5112, 5142, 5619, 5523, 5542,
    5930, 5113, 5143, 5620, 5524, 5544,
    5940, 5115, 5145, 5512, 5526, 5546, 5621,
    5945, 5116, 5146, 5513, 5527, 5547, 5622,
    5955, 5118, 5148, 5515, 5529, 5549, 5624,
    5950, 5117, 5147, 5514, 5528, 5548, 5623,
    5960, 5119, 5149, 5516, 5530, 5550, 5625
};

foreach(var expected in expectedGLs) {
    bool found = false;
    foreach(var line in lines) {
        if (line.Contains($"GlCode = {expected},")) {
            found = true;
            break;
        }
    }
    if (!found) {
        Console.WriteLine($"MISSING GL: {expected}");
    }
}
Console.WriteLine("Check complete.");
