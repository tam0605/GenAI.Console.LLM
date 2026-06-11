using DemoUnitTest_ConsoleApp;
using Xunit;

public class UnitTest_Generated
{
    [Fact]
    public void Add_ValidInputs_ReturnsCorrectSum()
    {
        // Arrange
        var calculator = new Calculator();

        // Act
        int result = calculator.Add(5, 3);

        // Assert
        Assert.Equal(8, result);
    }

    [Fact]
    public void Subtract_ValidInputs_ReturnsCorrectDifference()
    {
        // Arrange
        var calculator = new Calculator();

        // Act
        int result = calculator.Subtract(10, 4);

        // Assert
        Assert.Equal(6, result);
    }
}