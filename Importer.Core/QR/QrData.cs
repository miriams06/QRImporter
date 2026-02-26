using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Core.QR;

/// <summary>
/// Representa um documento fiscal estruturado a partir de um QR Code.
/// 
/// Este modelo é o núcleo do Ecrã Único de Validação (Módulo F),
/// sendo utilizado para edição manual, validação, exportação e histórico.
/// </summary>

public class QrData
{
    // Campos obrigatórios
    public string A { get; set; } = "";  // corresponde ao A (NIF do emitente)
    public string B { get; set; } = "";  // corresponde ao B (NIF do adquirente)
    public string C { get; set; } = "";  // corresponde ao C (País do adquirente)
    public string D { get; set; } = "";  // corresponde ao D (Tipo de documento)
    public string E { get; set; } = "";  // corresponde ao E (Estado do documento)
    public string F { get; set; } = "";  // corresponde ao F (Data do documento)
    public string G { get; set; } = "";  // corresponde ao G (Identificação única do documento)
    public string H { get; set; } = "";  // corresponde ao H (ATCUD)
    public string I1 { get; set; } = ""; // corresponde ao I1 (Espaço fiscal)

    // Campos opcionais
    public string I2 { get; set; } = ""; // corresponde ao I2 (Base tributável isenta de IVA)
    public string I3 { get; set; } = ""; // corresponde ao I3 (Base tributável de IVA à taxa reduzida)
    public string I4 { get; set; } = ""; // corresponde ao I4 (Total de IVA à taxa reduzida)
    public string I5 { get; set; } = ""; // corresponde ao I5 (Base tributável de IVA à taxa intermédia)
    public string I6 { get; set; } = ""; // corresponde ao I6 (Total de IVA à taxa intermédia)
    public string I7 { get; set; } = ""; // corresponde ao I7 (Base tributável de IVA à taxa normal)
    public string I8 { get; set; } = ""; // corresponde ao I8 (Total de IVA à taxa normal)

    public string J1 { get; set; } = ""; // corresponde ao J1 (Espaço fiscal)
    public string J2 { get; set; } = ""; // corresponde ao J2 (Base tributável isenta)
    public string J3 { get; set; } = ""; // corresponde ao J3 (Base tributável de IVA à taxa reduzida)
    public string J4 { get; set; } = ""; // corresponde ao J4 (Total de IVA à taxa reduzida)
    public string J5 { get; set; } = ""; // corresponde ao J5 (Base tributável de IVA à taxa intermédia)
    public string J6 { get; set; } = ""; // corresponde ao J6 (Total de IVA à taxa intermédia)
    public string J7 { get; set; } = ""; // corresponde ao J7 (Base tributável de IVA à taxa normal)
    public string J8 { get; set; } = ""; // corresponde ao J8 (Total de IVA à taxa normal)

    public string K1 { get; set; } = ""; // corresponde ao K1 (Espaço fiscal)
    public string K2 { get; set; } = ""; // corresponde ao K2 (Base tributável isenta)
    public string K3 { get; set; } = ""; // corresponde ao K3 (Base tributável de IVA à taxa reduzida)
    public string K4 { get; set; } = ""; // corresponde ao K4 (Total de IVA à taxa reduzida)
    public string K5 { get; set; } = ""; // corresponde ao K5 (Base tributável de IVA à taxa intermédia)
    public string K6 { get; set; } = ""; // corresponde ao K6 (Total de IVA à taxa intermédia)
    public string K7 { get; set; } = ""; // corresponde ao K7 (Base tributável de IVA à taxa normal)
    public string K8 { get; set; } = ""; // corresponde ao K8 (Total de IVA à taxa normal)

    public string L { get; set; } = "";  // corresponde ao L (Não sujeito/não tributável em IVA)
    public string M { get; set; } = "";  // corresponde ao M (Imposto do Selo)

    public string N { get; set; } = "";  // corresponde ao N (Total de impostos)
    public string O { get; set; } = "";  // corresponde ao O (Total do documento com impostos)
    public string P { get; set; } = "";  // corresponde ao P (Retenções na fonte)
    public string Q { get; set; } = "";  // corresponde ao Q (4 caracteres do Hash)
    public string R { get; set; } = "";  // corresponde ao R (Nº do certificado)
    public string S { get; set; } = "";  // corresponde ao S (Outras informações)

    /// <summary>
    /// Nome da empresa obtido por enriquecimento externo (nif.pt).
    /// </summary>
    public string NomeEmpresa { get; set; } = "";

    /// <summary>
    /// Morada da empresa obtida por enriquecimento externo (nif.pt).
    /// </summary>
    public string MoradaEmpresa { get; set; } = "";
}


public static class QrRepository
{
    public static List<QrData> QrCodes { get; set; } = new();
}

// Modelo para deserialização do JSON
public class NifRecord
{
    public long nif { get; set; }
    public string title { get; set; } = "";
    public string address { get; set; } = "";
}

public class NifInfo
{
    public string Nif { get; set; }
    public string Nome { get; set; }
    public string Morada { get; set; }
    public DateTime DataConsulta { get; set; } = DateTime.Now;
}
