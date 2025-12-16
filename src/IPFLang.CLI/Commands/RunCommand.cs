using System.ComponentModel;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using IPFLang.Engine;
using IPFLang.Parser;
using IPFLang.Evaluator;

namespace IPFLang.CLI.Commands;

public class RunCommand : Command<RunCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        [Description("Path to the IPFLang script file (.ipf)")]
        public string FilePath { get; set; } = string.Empty;

        [CommandOption("--inputs <FILE>")]
        [Description("JSON file containing input values (skips interactive mode)")]
        public string? InputsFile { get; set; }

        [CommandOption("-p|--provenance")]
        [Description("Show computation provenance (audit trail)")]
        public bool ShowProvenance { get; set; }

        [CommandOption("-c|--counterfactuals")]
        [Description("Show counterfactual analysis (what-if scenarios)")]
        public bool ShowCounterfactuals { get; set; }
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
        var inputValues = new List<IPFValue>();

        // Gather input values: use JSON file if provided, otherwise interactive mode
        if (!string.IsNullOrEmpty(settings.InputsFile))
        {
            inputValues = LoadInputsFromJson(settings.InputsFile, inputs);
            if (inputValues == null)
                return 1;
        }
        else
        {
            // Interactive mode (default)
            inputValues = GatherInteractiveInputs(inputs);
        }

        // Show input values
        AnsiConsole.MarkupLine("[blue]Input Values:[/]");
        var inputTable = new Table();
        inputTable.AddColumn("Input");
        inputTable.AddColumn("Value");
        foreach (var val in inputValues)
        {
            inputTable.AddRow(val.Name, FormatValue(val));
        }
        AnsiConsole.Write(inputTable);
        AnsiConsole.WriteLine();

        // Execute computation
        if (settings.ShowProvenance || settings.ShowCounterfactuals)
        {
            var provenance = settings.ShowCounterfactuals 
                ? calculator.ComputeWithCounterfactuals(inputValues)
                : calculator.ComputeWithProvenance(inputValues);

            DisplayProvenanceResults(provenance, settings.ShowCounterfactuals);
        }
        else
        {
            var (mandatory, optional, steps, returns) = calculator.Compute(inputValues);
            DisplayComputationResults(mandatory, optional, steps, returns, string.IsNullOrEmpty(settings.InputsFile));
        }

        return 0;
    }

    private List<IPFValue> GatherInteractiveInputs(List<DslInput> inputs)
    {
        var values = new List<IPFValue>();
        
        AnsiConsole.MarkupLine("[blue]Enter values for each input (press Enter for default):[/]");
        AnsiConsole.WriteLine();

        foreach (var input in inputs)
        {
            switch (input)
            {
                case DslInputNumber numInput:
                    var numValue = AnsiConsole.Ask(
                        $"[cyan]{input.Name}[/] ({input.Text}) [[{numInput.MinValue}-{numInput.MaxValue}]]:",
                        numInput.DefaultValue);
                    values.Add(new IPFValueNumber(input.Name, numValue));
                    break;

                case DslInputBoolean boolInput:
                    var boolValue = AnsiConsole.Confirm(
                        $"[cyan]{input.Name}[/] ({input.Text})",
                        boolInput.DefaultValue);
                    values.Add(new IPFValueBoolean(input.Name, boolValue));
                    break;

                case DslInputList listInput:
                    var choices = listInput.Items.Select(i => i.Symbol).ToArray();
                    var listValue = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title($"[cyan]{input.Name}[/] ({input.Text})")
                            .AddChoices(choices)
                            .UseConverter(c => $"{c} - {listInput.Items.First(i => i.Symbol == c).Value}"));
                    values.Add(new IPFValueString(input.Name, listValue));
                    break;

                case DslInputListMultiple multiInput:
                    var multiChoices = multiInput.Items.Select(i => i.Symbol).ToArray();
                    var multiValues = AnsiConsole.Prompt(
                        new MultiSelectionPrompt<string>()
                            .Title($"[cyan]{input.Name}[/] ({input.Text})")
                            .AddChoices(multiChoices)
                            .UseConverter(c => $"{c} - {multiInput.Items.First(i => i.Symbol == c).Value}"));
                    values.Add(new IPFValueStringList(input.Name, multiValues));
                    break;

                case DslInputDate dateInput:
                    var dateStr = AnsiConsole.Ask(
                        $"[cyan]{input.Name}[/] ({input.Text}) [[yyyy-MM-dd]]:",
                        dateInput.DefaultValue.ToString("yyyy-MM-dd"));
                    if (DateOnly.TryParse(dateStr, out var dateValue))
                        values.Add(new IPFValueDate(input.Name, dateValue));
                    else
                        values.Add(new IPFValueDate(input.Name, dateInput.DefaultValue));
                    break;

                case DslInputAmount amountInput:
                    var amtValue = AnsiConsole.Ask(
                        $"[cyan]{input.Name}[/] ({input.Text}) [{amountInput.Currency}]:",
                        amountInput.DefaultValue);
                    values.Add(new IPFValueNumber(input.Name, amtValue));
                    break;
            }
        }

        return values;
    }

    private List<IPFValue>? LoadInputsFromJson(string filePath, List<DslInput> inputs)
    {
        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Inputs file not found: {filePath}");
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var jsonDoc = JsonDocument.Parse(json);
            var values = new List<IPFValue>();

            foreach (var input in inputs)
            {
                if (jsonDoc.RootElement.TryGetProperty(input.Name, out var prop))
                {
                    switch (input)
                    {
                        case DslInputNumber:
                            values.Add(new IPFValueNumber(input.Name, prop.GetDecimal()));
                            break;
                        case DslInputBoolean:
                            values.Add(new IPFValueBoolean(input.Name, prop.GetBoolean()));
                            break;
                        case DslInputList:
                            values.Add(new IPFValueString(input.Name, prop.GetString() ?? ""));
                            break;
                        case DslInputListMultiple:
                            var list = prop.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
                            values.Add(new IPFValueStringList(input.Name, list));
                            break;
                        case DslInputDate:
                            if (DateOnly.TryParse(prop.GetString(), out var date))
                                values.Add(new IPFValueDate(input.Name, date));
                            break;
                        case DslInputAmount:
                            values.Add(new IPFValueNumber(input.Name, prop.GetDecimal()));
                            break;
                    }
                }
                else
                {
                    // Use default value
                    values.AddRange(GetDefaultValues(new List<DslInput> { input }));
                }
            }

            return values;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error parsing JSON:[/] {ex.Message}");
            return null;
        }
    }

    private List<IPFValue> GetDefaultValues(List<DslInput> inputs)
    {
        var values = new List<IPFValue>();

        foreach (var input in inputs)
        {
            switch (input)
            {
                case DslInputNumber numInput:
                    values.Add(new IPFValueNumber(input.Name, numInput.DefaultValue));
                    break;
                case DslInputBoolean boolInput:
                    values.Add(new IPFValueBoolean(input.Name, boolInput.DefaultValue));
                    break;
                case DslInputList listInput:
                    values.Add(new IPFValueString(input.Name, listInput.DefaultSymbol));
                    break;
                case DslInputListMultiple multiInput:
                    values.Add(new IPFValueStringList(input.Name, multiInput.DefaultSymbols.ToList()));
                    break;
                case DslInputDate dateInput:
                    values.Add(new IPFValueDate(input.Name, dateInput.DefaultValue));
                    break;
                case DslInputAmount amountInput:
                    values.Add(new IPFValueNumber(input.Name, amountInput.DefaultValue));
                    break;
            }
        }

        return values;
    }

    private string FormatValue(IPFValue value)
    {
        return value switch
        {
            IPFValueNumber n => n.Value.ToString("N2"),
            IPFValueBoolean b => b.Value.ToString(),
            IPFValueString s => s.Value,
            IPFValueStringList sl => string.Join(", ", sl.Value),
            IPFValueDate d => d.Value.ToString("yyyy-MM-dd"),
            _ => value.ToString() ?? ""
        };
    }

    private void DisplayComputationResults(decimal mandatory, decimal optional, IEnumerable<string> steps, IEnumerable<(string, string)> returns, bool isInteractive)
    {
        AnsiConsole.MarkupLine("[blue]Computation Results:[/]");
        AnsiConsole.WriteLine();

        var resultTable = new Table();
        resultTable.AddColumn("Category");
        resultTable.AddColumn(new TableColumn("Amount").RightAligned());

        resultTable.AddRow("Mandatory Fees", $"[green]{mandatory:N2}[/]");
        resultTable.AddRow("Optional Fees", $"[yellow]{optional:N2}[/]");
        resultTable.AddRow(new Rule());
        resultTable.AddRow("[bold]Grand Total[/]", $"[bold green]{mandatory + optional:N2}[/]");

        AnsiConsole.Write(resultTable);

        // Show returns
        var returnsList = returns.ToList();
        if (returnsList.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[blue]Returns:[/]");
            foreach (var (symbol, text) in returnsList)
            {
                AnsiConsole.MarkupLine($"  - {symbol}: {text}");
            }
        }

        // Show computation steps in a panel
        AnsiConsole.WriteLine();
        if (isInteractive && AnsiConsole.Confirm("Show detailed computation steps?", false))
        {
            AnsiConsole.WriteLine();
            var escapedSteps = steps.Select(s => Markup.Escape(s));
            var panel = new Panel(string.Join("\n", escapedSteps))
            {
                Header = new PanelHeader("[blue]Computation Steps[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);
        }
    }

    private void DisplayProvenanceResults(IPFLang.Provenance.ComputationProvenance provenance, bool showCounterfactuals)
    {
        AnsiConsole.MarkupLine("[blue]Computation Results with Provenance:[/]");
        AnsiConsole.WriteLine();

        // Summary
        var resultTable = new Table();
        resultTable.AddColumn("Category");
        resultTable.AddColumn(new TableColumn("Amount").RightAligned());
        resultTable.AddRow("Mandatory Fees", $"[green]{provenance.TotalMandatory:N2}[/]");
        resultTable.AddRow("Optional Fees", $"[yellow]{provenance.TotalOptional:N2}[/]");
        resultTable.AddRow(new Rule());
        resultTable.AddRow("[bold]Grand Total[/]", $"[bold green]{provenance.GrandTotal:N2}[/]");
        AnsiConsole.Write(resultTable);
        AnsiConsole.WriteLine();

        // Fee breakdown with provenance
        AnsiConsole.MarkupLine("[blue]Fee Breakdown (with audit trail):[/]");
        foreach (var feeProvenance in provenance.FeeProvenances)
        {
            var optional = feeProvenance.IsOptional ? " [dim](optional)[/]" : "";
            AnsiConsole.MarkupLine($"\n[cyan]{feeProvenance.FeeName}[/]{optional}: [green]{feeProvenance.TotalAmount:N2}[/]");
            
            foreach (var record in feeProvenance.Records)
            {
                var status = record.DidContribute ? "[green]USED[/]" : "[dim]SKIP[/]";
                var desc = record.YieldCondition != null 
                    ? $"YIELD {record.Expression} IF {record.YieldCondition}"
                    : $"YIELD {record.Expression}";
                AnsiConsole.MarkupLine($"  {status} {Markup.Escape(desc)}");
                if (record.DidContribute && record.Contribution != 0)
                {
                    AnsiConsole.MarkupLine($"    [dim]-> Added {record.Contribution:N2}[/]");
                }
            }
        }

        // Counterfactuals
        if (showCounterfactuals && provenance.Counterfactuals.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[blue]Counterfactual Analysis (What-If Scenarios):[/]");
            
            var cfTable = new Table();
            cfTable.AddColumn("Scenario");
            cfTable.AddColumn("Changed Input");
            cfTable.AddColumn("New Value");
            cfTable.AddColumn(new TableColumn("New Total").RightAligned());
            cfTable.AddColumn(new TableColumn("Difference").RightAligned());

            foreach (var cf in provenance.Counterfactuals.Take(10))
            {
                var diff = cf.Difference;
                var diffStr = diff >= 0 ? $"[red]+{diff:N2}[/]" : $"[green]{diff:N2}[/]";
                cfTable.AddRow(
                    Markup.Escape(cf.ToString()),
                    cf.InputName,
                    cf.AlternativeValue?.ToString() ?? "null",
                    cf.AlternativeTotal.ToString("N2"),
                    diffStr);
            }

            AnsiConsole.Write(cfTable);
        }
    }
}
