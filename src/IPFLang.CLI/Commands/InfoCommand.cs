using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using IPFLang.Engine;
using IPFLang.Parser;

namespace IPFLang.CLI.Commands;

public class InfoCommand : Command<InfoCommand.Settings>
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

        if (!calculator.Parse(sourceCode))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Failed to parse script. Run 'ipflang parse' for details.");
            return 1;
        }

        var inputs = calculator.GetInputs().ToList();
        var fees = calculator.GetFees().ToList();
        var groups = calculator.GetGroups().ToList();
        var returns = calculator.GetReturns().ToList();
        var verifications = calculator.GetVerifications().ToList();

        // Header
        AnsiConsole.Write(new Rule($"[blue]IPFLang Script Info[/]").LeftJustified());
        AnsiConsole.MarkupLine($"File: [cyan]{settings.FilePath}[/]");
        AnsiConsole.WriteLine();

        // Groups
        if (groups.Any())
        {
            AnsiConsole.Write(new Rule("[yellow]Groups[/]").LeftJustified());
            var groupTable = new Table();
            groupTable.AddColumn("Name");
            groupTable.AddColumn("Description");
            groupTable.AddColumn("Weight");
            
            foreach (var group in groups.OrderBy(g => g.Weight))
            {
                groupTable.AddRow(group.Name, group.Text, group.Weight.ToString());
            }
            AnsiConsole.Write(groupTable);
            AnsiConsole.WriteLine();
        }

        // Inputs
        AnsiConsole.Write(new Rule("[yellow]Inputs[/]").LeftJustified());
        var inputTable = new Table();
        inputTable.AddColumn("Name");
        inputTable.AddColumn("Type");
        inputTable.AddColumn("Description");
        inputTable.AddColumn("Default");
        inputTable.AddColumn("Constraints");

        foreach (var input in inputs)
        {
            var (type, defaultVal, constraints) = input switch
            {
                DslInputNumber n => ("NUMBER", n.DefaultValue.ToString(), $"{n.MinValue} - {n.MaxValue}"),
                DslInputBoolean b => ("BOOLEAN", b.DefaultValue.ToString(), "-"),
                DslInputList l => ("LIST", l.DefaultSymbol, $"{l.Items.Count} choices"),
                DslInputListMultiple m => ("MULTILIST", string.Join(", ", m.DefaultSymbols), $"{m.Items.Count} choices"),
                DslInputDate d => ("DATE", d.DefaultValue.ToString("yyyy-MM-dd"), $"{d.MinValue:yyyy-MM-dd} - {d.MaxValue:yyyy-MM-dd}"),
                DslInputAmount a => ($"AMOUNT<{a.Currency}>", a.DefaultValue.ToString(), "-"),
                _ => ("UNKNOWN", "-", "-")
            };
            inputTable.AddRow(input.Name, type, Markup.Escape(input.Text), defaultVal, constraints);
        }
        AnsiConsole.Write(inputTable);
        AnsiConsole.WriteLine();

        // Show LIST choices if any
        var lists = inputs.OfType<DslInputList>().ToList();
        var multiLists = inputs.OfType<DslInputListMultiple>().ToList();
        
        if (lists.Any() || multiLists.Any())
        {
            AnsiConsole.Write(new Rule("[yellow]List Choices[/]").LeftJustified());
            
            foreach (var list in lists)
            {
                AnsiConsole.MarkupLine($"[cyan]{list.Name}[/] (LIST):");
                foreach (var item in list.Items)
                {
                    var isDefault = item.Symbol == list.DefaultSymbol ? " [green](default)[/]" : "";
                    AnsiConsole.MarkupLine($"  - {item.Symbol}: {Markup.Escape(item.Value)}{isDefault}");
                }
                AnsiConsole.WriteLine();
            }
            
            foreach (var list in multiLists)
            {
                AnsiConsole.MarkupLine($"[cyan]{list.Name}[/] (MULTILIST):");
                foreach (var item in list.Items)
                {
                    var isDefault = list.DefaultSymbols.Contains(item.Symbol) ? " [green](default)[/]" : "";
                    AnsiConsole.MarkupLine($"  - {item.Symbol}: {Markup.Escape(item.Value)}{isDefault}");
                }
                AnsiConsole.WriteLine();
            }
        }

        // Fees
        AnsiConsole.Write(new Rule("[yellow]Fees[/]").LeftJustified());
        var feeTable = new Table();
        feeTable.AddColumn("Name");
        feeTable.AddColumn("Type");
        feeTable.AddColumn("Return Currency");
        feeTable.AddColumn("Cases");
        feeTable.AddColumn("Variables");

        foreach (var fee in fees)
        {
            var type = fee.Optional ? "[dim]OPTIONAL[/]" : "MANDATORY";
            var currency = fee.ReturnCurrency ?? "[dim]-[/]";
            var caseCount = fee.Cases.Count.ToString();
            var varCount = fee.Vars.Count.ToString();
            feeTable.AddRow(fee.Name, type, currency, caseCount, varCount);
        }
        AnsiConsole.Write(feeTable);
        AnsiConsole.WriteLine();

        // Returns
        if (returns.Any())
        {
            AnsiConsole.Write(new Rule("[yellow]Returns[/]").LeftJustified());
            foreach (var ret in returns)
            {
                AnsiConsole.MarkupLine($"  - [cyan]{ret.Symbol}[/]: {Markup.Escape(ret.Text)}");
            }
            AnsiConsole.WriteLine();
        }

        // Verifications
        if (verifications.Any())
        {
            AnsiConsole.Write(new Rule("[yellow]Verification Directives[/]").LeftJustified());
            foreach (var verify in verifications)
            {
                switch (verify)
                {
                    case DslVerifyComplete vc:
                        AnsiConsole.MarkupLine($"  - VERIFY COMPLETE FEE [cyan]{vc.FeeName}[/]");
                        break;
                    case DslVerifyMonotonic vm:
                        AnsiConsole.MarkupLine($"  - VERIFY MONOTONIC FEE [cyan]{vm.FeeName}[/] WITH RESPECT TO [cyan]{vm.WithRespectTo}[/] ({vm.Direction})");
                        break;
                }
            }
            AnsiConsole.WriteLine();
        }

        return 0;
    }
}
