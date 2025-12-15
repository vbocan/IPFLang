using System.ComponentModel;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using IPFLang.Engine;
using IPFLang.Parser;
using IPFLang.Evaluator;
using IPFLang.Composition;

namespace IPFLang.CLI.Commands;

public class ComposeCommand : Command<ComposeCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILES>")]
        [Description("IPFLang script files in inheritance order (parent first, children last)")]
        public string[] FilePaths { get; set; } = Array.Empty<string>();

        [CommandOption("--inputs <FILE>")]
        [Description("JSON file containing input values (skips interactive mode)")]
        public string? InputsFile { get; set; }

        [CommandOption("-p|--provenance")]
        [Description("Show computation provenance (audit trail)")]
        public bool ShowProvenance { get; set; }

        [CommandOption("-a|--analysis")]
        [Description("Show inheritance analysis (what is inherited vs. overridden)")]
        public bool ShowAnalysis { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.FilePaths.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] At least one IPFLang file is required.");
            return 1;
        }

        // Validate all files exist
        var filePaths = new List<string>();
        foreach (var filePath in settings.FilePaths)
        {
            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {filePath}");
                return 1;
            }
            filePaths.Add(filePath);
        }

        AnsiConsole.MarkupLine($"[dim]Files to compose ({filePaths.Count}):[/]");
        foreach (var fp in filePaths)
        {
            AnsiConsole.MarkupLine($"  [dim]{fp}[/]");
        }
        AnsiConsole.WriteLine();

        var parser = new DslParser();
        var registry = new JurisdictionRegistry();

        AnsiConsole.MarkupLine("[blue]Loading jurisdiction hierarchy...[/]");
        AnsiConsole.WriteLine();

        // Parse and register each jurisdiction
        string? previousId = null;
        var jurisdictionIds = new List<string>();

        for (int i = 0; i < filePaths.Count; i++)
        {
            var filePath = filePaths[i];
            var sourceCode = File.ReadAllText(filePath);
            
            var (parsed, errors) = parser.Parse(sourceCode, returnParsedScript: true);
            if (parsed == null || errors.Any())
            {
                AnsiConsole.MarkupLine($"[red]Error parsing {filePath}:[/]");
                foreach (var error in errors)
                {
                    AnsiConsole.MarkupLine($"  [red]{Markup.Escape(error)}[/]");
                }
                return 1;
            }

            // Use filename (without extension) as jurisdiction ID
            var jurisdictionId = Path.GetFileNameWithoutExtension(filePath);
            var jurisdictionName = parsed.Version?.Description ?? jurisdictionId;
            
            var jurisdiction = new Jurisdiction(
                jurisdictionId,
                jurisdictionName,
                parsed,
                previousId,  // Parent is the previous jurisdiction in the chain
                new Dictionary<string, string>
                {
                    ["FilePath"] = filePath,
                    ["Level"] = i.ToString()
                }
            );

            registry.Register(jurisdiction);
            jurisdictionIds.Add(jurisdictionId);

            var parentInfo = previousId != null ? $" (inherits from {previousId})" : " (root)";
            AnsiConsole.MarkupLine($"  [green]+[/] {jurisdictionId}{parentInfo}");

            previousId = jurisdictionId;
        }

        AnsiConsole.WriteLine();

        // Compose the final jurisdiction (last in the chain)
        var composer = new JurisdictionComposer(registry);
        var targetJurisdiction = jurisdictionIds.Last();
        var composed = composer.Compose(targetJurisdiction);

        AnsiConsole.MarkupLine($"[blue]Composed jurisdiction:[/] {targetJurisdiction}");
        AnsiConsole.MarkupLine($"[dim]Inheritance chain: {string.Join(" -> ", composed.AppliedJurisdictions)}[/]");
        AnsiConsole.WriteLine();

        // Show inheritance analysis if requested
        if (settings.ShowAnalysis)
        {
            ShowInheritanceAnalysis(composer, jurisdictionIds);
        }

        // Show composed script info
        AnsiConsole.MarkupLine($"[blue]Composed Script Summary:[/]");
        AnsiConsole.MarkupLine($"  Inputs: {composed.Script.Inputs.Count()}");
        AnsiConsole.MarkupLine($"  Fees: {composed.Script.Fees.Count()}");
        AnsiConsole.MarkupLine($"  Groups: {composed.Script.Groups.Count()}");
        AnsiConsole.MarkupLine($"  Returns: {composed.Script.Returns.Count()}");
        AnsiConsole.WriteLine();

        // Now execute the composed script
        var calculator = new DslCalculator(parser);
        calculator.LoadParsedScript(composed.Script);

        var inputs = calculator.GetInputs().ToList();
        var inputValues = new List<IPFValue>();

        // Gather input values
        if (!string.IsNullOrEmpty(settings.InputsFile))
        {
            inputValues = LoadInputsFromJson(settings.InputsFile, inputs);
            if (inputValues == null)
                return 1;
        }
        else
        {
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
        if (settings.ShowProvenance)
        {
            var provenance = calculator.ComputeWithProvenance(inputValues);
            DisplayProvenanceResults(provenance, composed.AppliedJurisdictions);
        }
        else
        {
            var (mandatory, optional, steps, returns) = calculator.Compute(inputValues);
            DisplayComputationResults(mandatory, optional, steps, returns, composed.AppliedJurisdictions);
        }

        return 0;
    }

    private void ShowInheritanceAnalysis(JurisdictionComposer composer, List<string> jurisdictionIds)
    {
        AnsiConsole.MarkupLine("[blue]Inheritance Analysis:[/]");
        AnsiConsole.WriteLine();

        foreach (var id in jurisdictionIds.Skip(1)) // Skip root, it has no inheritance
        {
            var analysis = composer.AnalyzeInheritance(id);
            
            AnsiConsole.MarkupLine($"[cyan]{id}:[/]");
            
            if (analysis.InheritedFees.Any())
            {
                AnsiConsole.MarkupLine($"  [green]Inherited fees:[/] {string.Join(", ", analysis.InheritedFees)}");
            }
            if (analysis.OverriddenFees.Any())
            {
                AnsiConsole.MarkupLine($"  [yellow]Overridden fees:[/] {string.Join(", ", analysis.OverriddenFees)}");
            }
            if (analysis.DefinedFees.Any())
            {
                AnsiConsole.MarkupLine($"  [blue]New fees:[/] {string.Join(", ", analysis.DefinedFees)}");
            }
            if (analysis.InheritedInputs.Any())
            {
                AnsiConsole.MarkupLine($"  [green]Inherited inputs:[/] {string.Join(", ", analysis.InheritedInputs)}");
            }
            if (analysis.OverriddenInputs.Any())
            {
                AnsiConsole.MarkupLine($"  [yellow]Overridden inputs:[/] {string.Join(", ", analysis.OverriddenInputs)}");
            }
            if (analysis.DefinedInputs.Any())
            {
                AnsiConsole.MarkupLine($"  [blue]New inputs:[/] {string.Join(", ", analysis.DefinedInputs)}");
            }
            
            AnsiConsole.MarkupLine($"  [dim]Code reuse: {analysis.ReusePercentage:F1}%[/]");
            AnsiConsole.WriteLine();
        }

        // Overall metrics
        var metrics = composer.CalculateMetrics();
        AnsiConsole.MarkupLine("[blue]Overall Composition Metrics:[/]");
        AnsiConsole.MarkupLine($"  Total jurisdictions: {metrics.TotalJurisdictions}");
        AnsiConsole.MarkupLine($"  With inheritance: {metrics.JurisdictionsWithInheritance} ({metrics.InheritancePercentage:F1}%)");
        AnsiConsole.MarkupLine($"  Total inherited fees: {metrics.TotalInheritedFees}");
        AnsiConsole.MarkupLine($"  Total overridden fees: {metrics.TotalOverriddenFees}");
        AnsiConsole.MarkupLine($"  Total new fees: {metrics.TotalDefinedFees}");
        AnsiConsole.MarkupLine($"  Overall code reuse: {metrics.ReusePercentage:F1}%");
        AnsiConsole.WriteLine();
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
                        $"[cyan]{input.Name}[/] ({input.Text}) [yyyy-MM-dd]:",
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

    private void DisplayComputationResults(decimal mandatory, decimal optional, IEnumerable<string> steps, IEnumerable<(string, string)> returns, List<string> appliedJurisdictions)
    {
        AnsiConsole.MarkupLine("[blue]Computation Results:[/]");
        AnsiConsole.MarkupLine($"[dim]Applied jurisdictions: {string.Join(" -> ", appliedJurisdictions)}[/]");
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

        // Show computation steps
        AnsiConsole.WriteLine();
        if (AnsiConsole.Confirm("Show detailed computation steps?", false))
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

    private void DisplayProvenanceResults(IPFLang.Provenance.ComputationProvenance provenance, List<string> appliedJurisdictions)
    {
        AnsiConsole.MarkupLine("[blue]Computation Results with Provenance:[/]");
        AnsiConsole.MarkupLine($"[dim]Applied jurisdictions: {string.Join(" -> ", appliedJurisdictions)}[/]");
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
    }
}
