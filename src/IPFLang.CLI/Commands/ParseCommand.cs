using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using IPFLang.Engine;
using IPFLang.Parser;

namespace IPFLang.CLI.Commands;

public class ParseCommand : Command<ParseCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        [Description("Path to the IPFLang script file (.ipf)")]
        public string FilePath { get; set; } = string.Empty;
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

        AnsiConsole.MarkupLine($"[blue]Parsing:[/] {settings.FilePath}");
        AnsiConsole.WriteLine();

        var success = calculator.Parse(sourceCode);

        if (success)
        {
            AnsiConsole.MarkupLine("[green]Script parsed and validated successfully[/]");
            AnsiConsole.WriteLine();

            // Show summary
            var inputs = calculator.GetInputs().ToList();
            var fees = calculator.GetFees().ToList();
            var groups = calculator.GetGroups().ToList();
            var verifications = calculator.GetVerifications().ToList();

            var table = new Table();
            table.AddColumn("Component");
            table.AddColumn("Count");
            
            table.AddRow("Inputs", inputs.Count.ToString());
            table.AddRow("Fees", fees.Count.ToString());
            table.AddRow("Groups", groups.Count.ToString());
            table.AddRow("Verifications", verifications.Count.ToString());
            
            AnsiConsole.Write(table);

            // Always show detailed output
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Inputs:[/]");
            foreach (var input in inputs)
            {
                var type = input switch
                {
                    DslInputNumber => "NUMBER",
                    DslInputBoolean => "BOOLEAN",
                    DslInputList => "LIST",
                    DslInputListMultiple => "MULTILIST",
                    DslInputDate => "DATE",
                    DslInputAmount => "AMOUNT",
                    _ => "UNKNOWN"
                };
                AnsiConsole.MarkupLine($"  - [cyan]{input.Name}[/] ({type}): {input.Text}");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Fees:[/]");
            foreach (var fee in fees)
            {
                var optional = fee.Optional ? " [dim](optional)[/]" : "";
                var currency = fee.ReturnCurrency != null ? $" -> {fee.ReturnCurrency}" : "";
                AnsiConsole.MarkupLine($"  - [cyan]{fee.Name}[/]{currency}{optional}");
            }

            return 0;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Parsing failed with errors:[/]");
            AnsiConsole.WriteLine();

            var errors = calculator.GetErrors().ToList();
            var typeErrors = calculator.GetTypeErrors().ToList();

            foreach (var error in errors)
            {
                AnsiConsole.MarkupLine($"  [red]-[/] {Markup.Escape(error)}");
            }

            foreach (var error in typeErrors)
            {
                AnsiConsole.MarkupLine($"  [red]-[/] Type error: {Markup.Escape(error.Message)}");
            }

            return 1;
        }
    }
}
