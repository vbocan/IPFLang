using IPFLang.Analysis;
using IPFLang.CurrencyConversion;
using IPFLang.Evaluator;
using IPFLang.Parser;
using IPFLang.Provenance;
using IPFLang.Types;

namespace IPFLang.Engine
{
    public class DslCalculator : IDslCalculator
    {
        private IDslParser Parser { get; set; }
        private ICurrencyConverter? CurrencyConverter { get; set; }
        private readonly CurrencyTypeChecker TypeChecker = new();
        private readonly CompletenessChecker CompletenessChecker = new();
        private readonly MonotonicityChecker MonotonicityChecker = new();
        private IEnumerable<TypeError> _typeErrors = Enumerable.Empty<TypeError>();

        public DslCalculator(IDslParser parser)
        {
            this.Parser = parser;
        }

        public bool Parse(string text)
        {
            _typeErrors = Enumerable.Empty<TypeError>();
            var parseResult = Parser.Parse(text);

            // If parsing succeeded, run type checker
            if (parseResult)
            {
                _typeErrors = TypeChecker.Check(Parser.GetInputs(), Parser.GetFees());
            }

            // Return true only if both parsing and type checking pass
            return parseResult && !_typeErrors.Any();
        }

        public IEnumerable<string> GetErrors() => Parser.GetErrors().Select(s => s.Item2);
        public IEnumerable<TypeError> GetTypeErrors() => _typeErrors;
        public IEnumerable<DslInput> GetInputs() => Parser.GetInputs();
        public IEnumerable<DslFee> GetFees() => Parser.GetFees();
        public IEnumerable<DslReturn> GetReturns() => Parser.GetReturns();
        public IEnumerable<DslGroup> GetGroups() => Parser.GetGroups();
        public IEnumerable<DslVerify> GetVerifications() => Parser.GetVerifications();

        public void SetCurrencyConverter(ICurrencyConverter converter)
        {
            CurrencyConverter = converter;
        }

        public (decimal, decimal, IEnumerable<string>, IEnumerable<(string, string)>) Compute(IEnumerable<IPFValue> InputValues)
        {
            decimal TotalMandatoryAmount = 0;
            decimal TotalOptionalAmount = 0;
            var ComputeSteps = new List<string>();

            foreach (var fee in Parser.GetFees())
            {
                if (fee.Optional)
                {
                    ComputeSteps.Add(string.Format("COMPUTING OPTIONAL FEE [{0}]", fee.Name));
                }
                else
                {
                    ComputeSteps.Add(string.Format("COMPUTING FEE [{0}]", fee.Name));
                }
                // Compose fee local variables
                var AllVars = new List<IPFValue>();
                AllVars.AddRange(InputValues);
                foreach (var fv in fee.Vars)
                {
                    var fv_val = DslEvaluator.EvaluateExpression(fv.ValueTokens.ToArray(), AllVars, fee.Name, CurrencyConverter);
                    var fee_val = new IPFValueNumber(fv.Name, fv_val);
                    AllVars.Add(fee_val);
                }
                // Proceed with computation
                decimal CurrentAmount = 0;
                ComputeSteps.Add(string.Format("Amount is initially {0}", CurrentAmount));
                foreach (DslFeeCase fc in fee.Cases.Cast<DslFeeCase>())
                {
                    var case_cond = DslEvaluator.EvaluateLogic(fc.Condition.ToArray(), AllVars, fee.Name, CurrencyConverter);
                    if (!case_cond)
                    {
                        ComputeSteps.Add(string.Format("Condition [{0}] is FALSE, skipping", string.Join(" ", fc.Condition)));
                        continue;
                    }
                    if (fc.Condition.Any()) ComputeSteps.Add(string.Format("Condition [{0}] is TRUE, proceeding with evaluating individual expressions", string.Join(" ", fc.Condition)));
                    foreach (var b in fc.Yields)
                    {
                        var cond_b = DslEvaluator.EvaluateLogic(b.Condition.ToArray(), AllVars, fee.Name, CurrencyConverter);
                        var val_b = DslEvaluator.EvaluateExpression(b.Values.ToArray(), AllVars, fee.Name, CurrencyConverter);
                        if (b.Condition.Any()) ComputeSteps.Add(string.Format("Condition: [{0}] is [{1}]", string.Join(" ", b.Condition), cond_b));
                        if (cond_b)
                        {
                            CurrentAmount += val_b;
                            ComputeSteps.Add(string.Format("Expression [{0}] evaluates to [{1}], therefore the new amount is {2}", string.Join(" ", b.Values), val_b, CurrentAmount.ToString("0.00")));
                        }
                    }
                }
                ComputeSteps.Add(string.Format("The final amount for fee {0} is {1}", fee.Name, CurrentAmount.ToString("0.00")));
                ComputeSteps.Add(string.Empty);
                if (fee.Optional)
                {
                    TotalOptionalAmount += CurrentAmount;
                }
                else
                {
                    TotalMandatoryAmount += CurrentAmount;
                }

            }
            ComputeSteps.Add(string.Format("Total amount for mandatory fees: [{0}]", TotalMandatoryAmount.ToString("0.00")));
            ComputeSteps.Add(string.Format("Total amount for optional fees: [{0}]", TotalOptionalAmount.ToString("0.00")));
            ComputeSteps.Add(string.Format("Grand total: [{0}]", (TotalMandatoryAmount + TotalOptionalAmount).ToString("0.00")));
            var Returns = Parser.GetReturns().Select(s => (s.Symbol, s.Text));
            return (TotalMandatoryAmount, TotalOptionalAmount, ComputeSteps, Returns);
        }

        public void Reset()
        {
            Parser.Reset();
        }

        public CompletenessReport VerifyCompleteness()
        {
            return CompletenessChecker.CheckCompleteness(Parser.GetInputs(), Parser.GetFees());
        }

        public MonotonicityReport VerifyMonotonicity(string feeName, string withRespectTo, MonotonicityDirection direction = MonotonicityDirection.NonDecreasing)
        {
            var fee = Parser.GetFees().FirstOrDefault(f => f.Name == feeName);
            if (fee == null)
                throw new ArgumentException($"Fee '{feeName}' not found");
            return MonotonicityChecker.CheckMonotonicity(fee, Parser.GetInputs(), withRespectTo, direction);
        }

        public VerificationResults RunVerifications()
        {
            var results = new VerificationResults();
            var fees = Parser.GetFees().ToList();
            var inputs = Parser.GetInputs().ToList();

            foreach (var verify in Parser.GetVerifications())
            {
                try
                {
                    switch (verify)
                    {
                        case DslVerifyComplete vc:
                            var fee = fees.FirstOrDefault(f => f.Name == vc.FeeName);
                            if (fee == null)
                            {
                                results.Errors.Add($"Fee '{vc.FeeName}' not found for VERIFY COMPLETE");
                                continue;
                            }
                            var domains = new DomainAnalyzer().ExtractDomains(inputs).ToList();
                            var report = CompletenessChecker.CheckFeeCompleteness(fee, domains);
                            results.CompletenessReports.Add(report);
                            break;

                        case DslVerifyMonotonic vm:
                            var mFee = fees.FirstOrDefault(f => f.Name == vm.FeeName);
                            if (mFee == null)
                            {
                                results.Errors.Add($"Fee '{vm.FeeName}' not found for VERIFY MONOTONIC");
                                continue;
                            }
                            if (!Enum.TryParse<MonotonicityDirection>(vm.Direction, out var direction))
                            {
                                results.Errors.Add($"Invalid direction '{vm.Direction}' for VERIFY MONOTONIC");
                                continue;
                            }
                            var mReport = MonotonicityChecker.CheckMonotonicity(mFee, inputs, vm.WithRespectTo, direction);
                            results.MonotonicityReports.Add(mReport);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    results.Errors.Add($"Error processing {verify}: {ex.Message}");
                }
            }

            return results;
        }

        public ComputationProvenance ComputeWithProvenance(IEnumerable<IPFValue> inputValues)
        {
            var collector = new ProvenanceCollector(CurrencyConverter);
            return collector.ComputeWithProvenance(Parser.GetFees(), inputValues);
        }

        public ComputationProvenance ComputeWithCounterfactuals(IEnumerable<IPFValue> inputValues)
        {
            var provenance = ComputeWithProvenance(inputValues);

            var counterfactualEngine = new CounterfactualEngine(CurrencyConverter);
            var counterfactuals = counterfactualEngine.GenerateCounterfactuals(
                Parser.GetFees(),
                Parser.GetInputs(),
                inputValues,
                provenance.GrandTotal);

            foreach (var cf in counterfactuals)
            {
                provenance.Counterfactuals.Add(cf);
            }

            return provenance;
        }
    }
}
