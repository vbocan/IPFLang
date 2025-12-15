using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using IPFLang.Engine;
using IPFLang.Parser;
using IPFLang.Analysis;

namespace IPFLang.CLI.Commands;

public class VerifyCommand : Command<VerifyCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        [Description("Path to the IPFLang script file (.ipf)")]
        public string FilePath { get; set; } = string.Empty;

        [CommandOption("--completeness")]
        [Description("Run completeness verification only")]
        public bool CompletenessOnly { get; set; }

        [CommandOption("--monotonicity")]
        [Description("Run monotonicity verification only")]
        public bool MonotonicityOnly { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {settings.FilePath}");
            return 1;
        }

        var sourceCode = File.ReadAllText(settings.FilePath);
        var parser = new DslParser();
        var calculator = new DslCalculator(parser);

        if (!calculator.Parse(sourceCode))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Failed to parse script. Run 'ipflang parse' for details.");
            return 1;
        }

        var verifications = calculator.GetVerifications().ToList();
        var runAll = !settings.CompletenessOnly && !settings.MonotonicityOnly;
        var hasErrors = false;

        AnsiConsole.MarkupLine($"[blue]Running Verification Checks:[/] {settings.FilePath}");
        AnsiConsole.WriteLine();

        // Run embedded VERIFY directives
        if (verifications.Any())
        {
            AnsiConsole.MarkupLine("[yellow]Embedded Verification Directives:[/]");
            var results = calculator.RunVerifications();

            foreach (var report in results.CompletenessReports)
            {
                DisplayCompletenessReport(report);
                if (!report.IsComplete) hasErrors = true;
            }

            foreach (var report in results.MonotonicityReports)
            {
                DisplayMonotonicityReport(report);
                if (!report.IsMonotonic) hasErrors = true;
            }

            foreach (var error in results.Errors)
            {
                AnsiConsole.MarkupLine($"  [red]FAIL[/] {error}");
                hasErrors = true;
            }

            AnsiConsole.WriteLine();
        }

        // Run comprehensive completeness check
        if (runAll || settings.CompletenessOnly)
        {
            AnsiConsole.MarkupLine("[yellow]Comprehensive Completeness Analysis:[/]");
            var completenessReport = calculator.VerifyCompleteness();
            
            var table = new Table();
            table.AddColumn("Fee");
            table.AddColumn("Status");
            table.AddColumn("Checked");
            table.AddColumn("Gaps");

            foreach (var feeReport in completenessReport.FeeReports)
            {
                var status = feeReport.IsComplete 
                    ? "[green]Complete[/]" 
                    : "[yellow]Incomplete[/]";
                var checked_count = feeReport.TotalCombinationsChecked.ToString();
                var gaps = feeReport.Gaps.Any() 
                    ? string.Join(", ", feeReport.Gaps.Take(3).Select(g => g.ToString())) + (feeReport.Gaps.Count > 3 ? "..." : "")
                    : "-";
                
                table.AddRow(feeReport.FeeName, status, checked_count, Markup.Escape(gaps));
                
                // Note: Comprehensive analysis is informational, not an error
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        // Summary - only fail on embedded verification directive failures
        if (hasErrors)
        {
            AnsiConsole.MarkupLine("[red]Verification completed with errors[/]");
            return 1;
        }
        else
        {
            AnsiConsole.MarkupLine("[green]All verification checks passed[/]");
            return 0;
        }
    }

    private void DisplayCompletenessReport(FeeCompletenessReport report)
    {
        var status = report.IsComplete ? "[green]PASS[/]" : "[red]FAIL[/]";
        AnsiConsole.MarkupLine($"  {status} COMPLETE {report.FeeName}: {report.TotalCombinationsChecked} combinations checked");
        
        if (report.Gaps.Any())
        {
            foreach (var gap in report.Gaps.Take(5))
            {
                AnsiConsole.MarkupLine($"      [dim]Gap: {Markup.Escape(gap.ToString())}[/]");
            }
            if (report.Gaps.Count > 5)
            {
                AnsiConsole.MarkupLine($"      [dim]...and {report.Gaps.Count - 5} more gaps[/]");
            }
        }
    }

    private void DisplayMonotonicityReport(MonotonicityReport report)
    {
        var status = report.IsMonotonic ? "[green]PASS[/]" : "[red]FAIL[/]";
        AnsiConsole.MarkupLine($"  {status} MONOTONIC {report.FeeName} w.r.t. {report.WithRespectTo}: {report.ExpectedDirection}");
        
        if (report.Violations.Any())
        {
            foreach (var violation in report.Violations.Take(3))
            {
                AnsiConsole.MarkupLine($"      [dim]Violation: {Markup.Escape(violation.ToString())}[/]");
            }
            if (report.Violations.Count > 3)
            {
                AnsiConsole.MarkupLine($"      [dim]...and {report.Violations.Count - 3} more violations[/]");
            }
        }
    }
}
