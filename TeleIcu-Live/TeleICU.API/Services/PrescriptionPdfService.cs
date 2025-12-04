using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Elements;
using System.IO;
using TeleICU.API.DTOs.CaseDto;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Services;

public class PrescriptionPdfService : IPrescriptionPdfService
{
    private readonly ICaseRepository _caseRepository;
    private readonly ICallRepository _callRepository;
    private readonly ILogger<PrescriptionPdfService> _logger;

    public PrescriptionPdfService(
        ICaseRepository caseRepository,
        ICallRepository callRepository,
        ILogger<PrescriptionPdfService> logger)
    {
        _caseRepository = caseRepository;
        _callRepository = callRepository;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePrescriptionPdfAsync(string callId, int caseId)
    {
        // Get case data
        var caseData = await _caseRepository.GetCaseViewAsync(caseId);
        if (caseData == null)
            throw new Exception("Case not found");

        // Get call/specialist data
        var callData = await _callRepository.GetCallDataForPrescriptionAsync(callId);
        if (callData == null)
            throw new Exception("Call data not found");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            // left: logos
                            row.RelativeItem().Column(c =>
                            {
                                var logoPath = ResolveLogoPath();
                                if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                                {
                                    c.Item().Height(28).Image(logoPath);
                                }
                                else
                                {
                                    c.Item().Text("eSanjeevaniICU").FontSize(18).Bold();
                                }
                            });

                            // right: spoke + date/patient/consultation
                            row.RelativeItem().AlignRight().Column(rc =>
                            {
                                rc.Item().AlignRight().Text(caseData.ICU).FontSize(12).Bold();
                                rc.Item().AlignRight().Text(caseData.Address).FontSize(9);
                                rc.Item().AlignRight().Text($"{callData.ConsultationDateTime:dd MMMM yyyy HH:mm:ss}").FontSize(10).Bold();
                                rc.Item().AlignRight().Text($"Patient ID: {caseData.PatientId}").FontSize(10);
                                rc.Item().AlignRight().Text($"Consultation ID: {callData.ConsultationId}").FontSize(10).Bold();
                            });
                        });
                    });

                page.Content()
                    .PaddingTop(10)
                    .Column(column =>
                    {
                        column.Spacing(10);
                        // Patient line
                        column.Item().Border(1).Padding(6).Text($"{caseData.PatientName} | {caseData.AgeYears}Y | {caseData.Gender}").Bold();

                        // Vitals
                        if (caseData.LatestVitals != null)
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(140);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(140);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(140);
                                    columns.RelativeColumn();
                                });
                                var vitals = caseData.LatestVitals;

                                AddVitalCell(table, "Vitals (" + caseData.LatestVitals.CreatedDate.ToString("dd MMM yyyy HH:mm:ss") + ")", true, 6);

                                AddVitalPair(table, "SpO2", $"{vitals.OxygenSaturation:F2} %");
                                AddVitalPair(table, "HR", $"{vitals.HeartRate}");
                                AddVitalPair(table, "Temp", $"{vitals.Temperature:F2} °F");
                                AddVitalPair(table, "SBP", $"{vitals.BloodPressureSYS} mmHg");
                                AddVitalPair(table, "DBP", $"{vitals.BloodPressureDIA} mmHg");
                                AddVitalPair(table, "RR", $"{vitals.RespiratoryRate} /m");
                                AddVitalPair(table, "RAP", $"{vitals.RightAtrialPressure:F2} mmHg");
                                AddVitalPair(table, "Blood Glucose", $"{vitals.BloodGlucose:F2} mg/DL");
                            });
                        }

                        // Pre-Admit Medication
                        if (caseData.PreAdmitMedications.Any())
                        {
                            column.Item().Text("Pre-Admit Medication").FontSize(12).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(50);
                                    columns.ConstantColumn(50);
                                    columns.ConstantColumn(60);
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Sr. No.").FontSize(9).Bold();
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Name").FontSize(9).Bold();
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Freq.").FontSize(9).Bold();
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Dose").FontSize(9).Bold();
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Type").FontSize(9).Bold();
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Duration").FontSize(9).Bold();
                                });

                                int srNo = 1;
                                foreach (var med in caseData.PreAdmitMedications)
                                {
                                    BodyCell(table, srNo++.ToString());
                                    BodyCell(table, med.Medicine);
                                    BodyCell(table, med.Frequency ?? "");
                                    BodyCell(table, med.Dose ?? "");
                                    BodyCell(table, med.Type ?? "");
                                    BodyCell(table, $"{med.DurationValue} {med.DurationType}");
                                }
                            });
                        }

                        // Chief Complaints
                        if (!string.IsNullOrEmpty(caseData.ChiefComplaint))
                        {
                            column.Item().Text("Chief Complaints").FontSize(12).Bold();
                            column.Item().Border(1).Height(20).Padding(4).Text(caseData.ChiefComplaint);
                        }

                        // Query
                        if (!string.IsNullOrEmpty(caseData.Query))
                        {
                            column.Item().Text("Query").FontSize(12).Bold();
                            column.Item().Border(1).Height(20).Padding(4).Text(caseData.Query);
                        }

                        // Sender line
                        column.Item().Text($"Sender: {caseData.NurseDetails.NurseName}  {caseData.NurseDetails.NurseAddress}").FontSize(10).Bold();

                        // Prescription (Prescribed Medications)
                        if (caseData.PrescribedMedications.Any())
                        {
                            column.Item().Text("Prescription").FontSize(12).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(50);
                                    columns.ConstantColumn(50);
                                    columns.ConstantColumn(60);
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Sr. No.").FontSize(9).Bold();
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Name").FontSize(9).Bold();
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Freq.").FontSize(9).Bold();
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Dose").FontSize(9).Bold();
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Type").FontSize(9).Bold();
                                    header.Cell().Background("#eef3f7").Border(1).Padding(3).Text("Duration").FontSize(9).Bold();
                                });

                                int srNo = 1;
                                foreach (var med in caseData.PrescribedMedications)
                                {
                                    BodyCell(table, srNo++.ToString());
                                    BodyCell(table, med.Medicine);
                                    BodyCell(table, med.Frequency ?? "");
                                    BodyCell(table, med.Dose ?? "");
                                    BodyCell(table, med.Type ?? "");
                                    BodyCell(table, $"{med.DurationValue} {med.DurationType}");
                                }
                            });
                        }

                        // Examination
                        if (!string.IsNullOrEmpty(caseData.Examination))
                        {
                            column.Item().Text("Examination").FontSize(12).Bold();
                            column.Item().Border(1).Height(24).Padding(4).Text(caseData.Examination);
                        }

                        // Advice
                        if (!string.IsNullOrEmpty(caseData.Advice))
                        {
                            column.Item().Text("Advice").FontSize(12).Bold();
                            column.Item().Border(1).Height(24).Padding(4).Text(caseData.Advice);
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text("Powered By ISOFT")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);
            });
        });

        return document.GeneratePdf();
    }

    // removed HeaderCell helper to avoid runtime issues with dynamic + extension methods

    private void BodyCell(TableDescriptor table, string text)
        => table.Cell().Border(1).Padding(3).Text(text).FontSize(9);

    private void AddVitalCell(TableDescriptor table, string text, bool header, int span)
    {
        var cell = table.Cell().ColumnSpan((uint)span).Border(1).Padding(4).Background(header ? "#eef3f7" : Colors.White);
        var textDescriptor = cell.Text(text).FontSize(header ? 10 : 9);
        if (header) textDescriptor.Bold();
    }

    private void AddVitalPair(TableDescriptor table, string label, string value)
    {
        table.Cell().Border(1).Padding(3).Background("#eef3f7").Text(label).FontSize(9).Bold();
        table.Cell().Border(1).Padding(3).Text(value).FontSize(9);
    }

    private string? ResolveLogoPath()
    {
        var root = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(root, "wwwroot", "uploads", "esanjeevaniICU.png"),
            Path.Combine(root, "wwwroot", "assets", "esanjeevaniICU.png"),
            Path.Combine(root, "wwwroot", "logos", "esanjeevaniICU.png"),
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}

