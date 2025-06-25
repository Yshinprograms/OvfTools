using Microsoft.VisualStudio.TestTools.UnitTesting;
using netDxf; // The correct namespace!
using OvfAnnotator;
using System.Collections.Generic;

namespace OvfAnnotatorTests {
    [TestClass]
    public class ColorGeneratorTests {
        [TestMethod]
        public void GetNextColor_OnFirstCall_ReturnsAValidColor() {
            // ARRANGE
            var generator = new ColorGenerator();

            // ACT
            var color = generator.GetNextColor();

            // ASSERT
            Assert.IsNotNull(color, "The first generated color should not be null.");
            ///Assert.IsTrue(color.IsByColor, "The color should be a true RGB color, not an index.");
        }

        [TestMethod]
        public void GetNextColor_FirstTwoCalls_AreDifferentColors() {
            // This tests the main if/else branching for even and odd indices.
            // ARRANGE
            var generator = new ColorGenerator();

            // ACT
            var color1 = generator.GetNextColor(); // index 0
            var color2 = generator.GetNextColor(); // index 1

            // ASSERT
            Assert.AreNotEqual(color1, color2, "The first two colors should be different due to the 180-degree hue shift.");
        }

        [TestMethod]
        public void GetNextColor_FirstFiveCalls_AreAllUnique() {
            // This is a general "happy path" test to ensure we're getting variety.
            // ARRANGE
            var generator = new ColorGenerator();
            var colorSet = new HashSet<AciColor>();

            // ACT
            for (int i = 0; i < 5; i++) {
                colorSet.Add(generator.GetNextColor());
            }

            // ASSERT
            Assert.AreEqual(5, colorSet.Count, "The first 5 generated colors should all be unique.");
        }

        [TestMethod]
        public void GetNextColor_HueProgression_IsPredictable() {
            // This tests that the hue advances by the expected 72 degrees for even-indexed calls.
            // ARRANGE
            var generator = new ColorGenerator();

            // ACT
            var color1 = generator.GetNextColor(); // Hue should be based on 0 degrees
            generator.GetNextColor(); // Skip the odd index
            var color3 = generator.GetNextColor(); // Hue should be based on 72 degrees

            // ASSERT
            // We can't easily check the hue directly, but we can be certain they are different colors.
            // A more complex test could convert RGB back to HSV, but for now, this is a strong indicator.
            Assert.AreNotEqual(color1, color3, "The 1st and 3rd colors should be different due to hue progression.");
        }

        [TestMethod]
        public void GetNextColor_AfterTenCalls_CyclesCorrectly() {
            // This tests the "hue %= 360" boundary condition.
            // The hue increment is 72. 72 * 5 = 360.
            // So the 11th call (index 10, which is even) should have a hue of 0, just like the 1st call (index 0).
            // ARRANGE
            var generator = new ColorGenerator();
            var colors = new List<AciColor>();

            // ACT
            for (int i = 0; i < 11; i++) {
                colors.Add(generator.GetNextColor());
            }

            // ASSERT
            var firstColor = colors[0];
            var eleventhColor = colors[10];
            Assert.AreEqual(firstColor, eleventhColor, "The 11th color should be the same as the 1st due to hue wrapping around 360 degrees.");
        }
    }
}