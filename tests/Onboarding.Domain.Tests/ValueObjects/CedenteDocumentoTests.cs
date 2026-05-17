using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.Domain.Tests.ValueObjects;

public class CedenteDocumentoTests
{
    [Fact]
    public void Pf_WithValidCpf_CreatesPfVariant()
    {
        var doc = CedenteDocumento.Pf(Cpf.Create("52998224725"));

        doc.IsPf.ShouldBeTrue();
        doc.IsPj.ShouldBeFalse();
    }

    [Fact]
    public void Pj_WithValidCnpj_CreatesPjVariant()
    {
        var doc = CedenteDocumento.Pj(Cnpj.Create("11222333000181"));

        doc.IsPj.ShouldBeTrue();
        doc.IsPf.ShouldBeFalse();
    }

    [Fact]
    public void Match_OnPf_ExecutesPfBranch()
    {
        var doc = CedenteDocumento.Pf(Cpf.Create("52998224725"));

        var result = doc.Match(
            pf => $"PF:{pf.Cpf.Value}",
            pj => $"PJ:{pj.Cnpj.Value}");

        result.ShouldBe("PF:52998224725");
    }

    [Fact]
    public void Match_OnPj_ExecutesPjBranch()
    {
        var doc = CedenteDocumento.Pj(Cnpj.Create("11222333000181"));

        var result = doc.Match(
            pf => $"PF:{pf.Cpf.Value}",
            pj => $"PJ:{pj.Cnpj.Value}");

        result.ShouldStartWith("PJ:");
    }

    [Fact]
    public void Pf_AsPessoaFisica_AccessCpf()
    {
        var cpf = Cpf.Create("52998224725");
        var doc = CedenteDocumento.Pf(cpf);

        var pfDoc = (CedenteDocumento.PessoaFisica)doc;
        pfDoc.Cpf.ShouldBe(cpf);
    }

    [Fact]
    public void Pj_AsPessoaJuridica_AccessCnpj()
    {
        var cnpj = Cnpj.Create("11222333000181");
        var doc = CedenteDocumento.Pj(cnpj);

        var pjDoc = (CedenteDocumento.PessoaJuridica)doc;
        pjDoc.Cnpj.ShouldBe(cnpj);
    }

    [Fact]
    public void TwoPfWithSameCpf_AreEqual()
    {
        var doc1 = CedenteDocumento.Pf(Cpf.Create("52998224725"));
        var doc2 = CedenteDocumento.Pf(Cpf.Create("52998224725"));

        doc1.ShouldBe(doc2);
    }

    [Fact]
    public void TwoPjWithSameCnpj_AreEqual()
    {
        var doc1 = CedenteDocumento.Pj(Cnpj.Create("11222333000181"));
        var doc2 = CedenteDocumento.Pj(Cnpj.Create("11222333000181"));

        doc1.ShouldBe(doc2);
    }

    [Fact]
    public void PfAndPj_AreNotEqual()
    {
        var pf = CedenteDocumento.Pf(Cpf.Create("52998224725"));
        var pj = CedenteDocumento.Pj(Cnpj.Create("11222333000181"));

        pf.ShouldNotBe(pj);
    }
}