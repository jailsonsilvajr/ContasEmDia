using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.ValueObjects;

public class NoteTests
{
    [Fact]
    public void Constructor_Null_IsAcceptedAndRoundTrips()
    {
        var note = new Note(null);

        Assert.Null(note.GetValue());
    }

    [Fact]
    public void Constructor_EmptyString_IsAcceptedAndRoundTrips()
    {
        var note = new Note("");

        Assert.Equal("", note.GetValue());
    }

    [Fact]
    public void Constructor_ArbitraryFreeText_IsAcceptedAndRoundTrips()
    {
        var note = new Note("Pagar via débito automático");

        Assert.Equal("Pagar via débito automático", note.GetValue());
    }
}
