using System.Text;
using Iroute.Commerce.Application.Csv;
using Iroute.Commerce.Application.Exceptions;

namespace Iroute.Commerce.Tests;

public class CommerceCsvParserTests
{
    private const string Header = "pc_processdate,pc_codcomred,pc_nomcomred,pc_tipdoc,pc_numdoc,pc_direccion,pc_ciudad,pc_telefono,pc_email";
    private static readonly DateOnly FileDate = new(2026, 10, 16);

    private readonly CommerceCsvParser _parser = new();

    private static MemoryStream ToStream(string content) => new(Encoding.UTF8.GetBytes(content));

    private Task<IReadOnlyList<Application.Models.CommerceRecord>> Parse(string csv)
        => _parser.ParseAsync(ToStream(csv), FileDate, CancellationToken.None);

    [Fact]
    public async Task ParseAsync_ValidFile_ReturnsRecords()
    {
        var csv = $"{Header}\n2026-10-16,C1,\"Comercio, S.A.\",RUC,123,Dir,Quito,099,a@b.com\n2026-10-16,C2,,CED,12A,,,,";

        var records = await Parse(csv);

        Assert.Equal(2, records.Count);
        Assert.Equal(FileDate, records[0].PcProcessDate);
        Assert.Equal("Comercio, S.A.", records[0].PcNomComRed);
        Assert.Null(records[1].PcNomComRed);
        Assert.Equal("12A", records[1].PcNumDoc);
    }

    [Fact]
    public async Task ParseAsync_SemicolonDelimiter_IsDetected()
    {
        var csv = $"{Header.Replace(',', ';')}\n16/10/2026;C1;Comercio;RUC;123;Dir;Quito;099;a@b.com";

        var records = await Parse(csv);

        Assert.Single(records);
        Assert.Equal("Comercio", records[0].PcNomComRed);
    }

    [Theory]
    [InlineData("")]
    [InlineData(Header)]
    public async Task ParseAsync_EmptyOrHeaderOnly_Throws(string csv)
    {
        await Assert.ThrowsAsync<ValidationException>(() => Parse(csv));
    }

    [Fact]
    public async Task ParseAsync_MissingColumns_Throws()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Parse("pc_processdate,pc_nomcomred\n2026-10-16,X"));

        Assert.Contains(ex.Errors, e => e.Contains("pc_numdoc"));
    }

    [Fact]
    public async Task ParseAsync_InvalidDate_ReportsLine()
    {
        var csv = $"{Header}\n2026-99-99,C1,Comercio,RUC,123,Dir,Quito,099,a@b.com";

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Parse(csv));

        Assert.Contains(ex.Errors, e => e.StartsWith("Línea 2"));
    }

    [Fact]
    public async Task ParseAsync_ProcessDateDifferentFromFileName_Throws()
    {
        // Caso: se renombra el archivo a otra fecha pero el contenido es el mismo.
        var csv = $"{Header}\n2026-10-15,C1,Comercio,RUC,123,Dir,Quito,099,a@b.com";

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Parse(csv));

        Assert.Contains(ex.Errors, e => e.Contains("no coincide con la fecha del archivo"));
    }

    [Fact]
    public async Task ParseAsync_DuplicatedCommerceCode_ReportsBothLines()
    {
        var csv = $"{Header}\n2026-10-16,C1,Comercio,RUC,123,,,,\n2026-10-16,c1,Otro,RUC,456,,,,";

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Parse(csv));

        Assert.Contains(ex.Errors, e => e.StartsWith("Línea 3") && e.Contains("línea 2"));
    }

    [Fact]
    public async Task ParseAsync_MissingCommerceCode_Throws()
    {
        var csv = $"{Header}\n2026-10-16,,Comercio,RUC,123,,,,";

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Parse(csv));

        Assert.Contains(ex.Errors, e => e.Contains("'pc_codcomred' es obligatorio"));
    }

    [Fact]
    public void SplitLine_HandlesEscapedQuotes()
    {
        var fields = CommerceCsvParser.SplitLine("a,\"b \"\"c\"\"\",d", ',');

        Assert.Equal(["a", "b \"c\"", "d"], fields);
    }
}
