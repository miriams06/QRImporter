using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Core.QR;

/// <summary>
/// Responsável por interpretar o texto bruto de um QR Code fiscal
/// e convertê-lo num objeto estruturado (QrData).
/// 
/// Utilizado no processo inicial de leitura e no reprocessamento do QR
/// (Módulo F2 — Reprocessamento QR).
/// </summary>
/// /// 
/// TODO (Módulo F2):
/// Implementar regras para não sobrescrever campos editados manualmente
/// quando o QR é reprocessado.
public static class QrParser
{
    /// <summary>
    /// Faz o parsing do texto do QR Code de acordo com a especificação da AT.
    /// </summary>
    /// <param name="qrText">Texto bruto extraído do QR.</param>
    /// <returns>Objeto QrData com os campos preenchidos.</returns>
    public static QrData Parse(string qrText)
    {
        var qrData = new QrData();

        if (string.IsNullOrWhiteSpace(qrText))
            return qrData;

        var fields = qrText.Split('*');

        foreach (var field in fields)
        {
            var parts = field.Split(':', 2); // só no primeiro ":" para manter valores com ":"
            if (parts.Length != 2)
                continue;

            var code = parts[0].Trim();
            var value = parts[1].Trim();

            switch (code)
            {
                case "A": qrData.A = value; break;
                case "B": qrData.B = value; break;
                case "C": qrData.C = value; break;
                case "D": qrData.D = value; break;
                case "E": qrData.E = value; break;
                case "F": qrData.F = value; break;
                case "G": qrData.G = value; break;
                case "H": qrData.H = value; break;
                case "I1": qrData.I1 = value; break;
                case "I2": qrData.I2 = value; break;
                case "I3": qrData.I3 = value; break;
                case "I4": qrData.I4 = value; break;
                case "I5": qrData.I5 = value; break;
                case "I6": qrData.I6 = value; break;
                case "I7": qrData.I7 = value; break;
                case "I8": // tolerância: às vezes vem "18" (OCR/typo) em vez de "I8"
                case "18": qrData.I8 = value;break;
                case "J1": qrData.J1 = value; break;
                case "J2": qrData.J2 = value; break;
                case "J3": qrData.J3 = value; break;
                case "J4": qrData.J4 = value; break;
                case "J5": qrData.J5 = value; break;
                case "J6": qrData.J6 = value; break;
                case "J7": qrData.J7 = value; break;
                case "J8": qrData.J8 = value; break;
                case "K1": qrData.K1 = value; break;
                case "K2": qrData.K2 = value; break;
                case "K3": qrData.K3 = value; break;
                case "K4": qrData.K4 = value; break;
                case "K5": qrData.K5 = value; break;
                case "K6": qrData.K6 = value; break;
                case "K7": qrData.K7 = value; break;
                case "K8": qrData.K8 = value; break;
                case "L": qrData.L = value; break;
                case "M": qrData.M = value; break;
                case "N": qrData.N = value; break;
                case "O": // tolerância: às vezes vem "0" (zero) em vez de "O"
                case "0": qrData.O = value; break;
                case "P": qrData.P = value; break;
                case "Q": qrData.Q = value; break;
                case "R": qrData.R = value; break;
                case "S": qrData.S = value; break;
            }
        }

        return qrData;
    }
}
