using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartCampus.Business.Services
{
    public interface IGeneticSchedulingService
    {
        Task<GeneticScheduleResultDto> GenerateWithGeneticAlgorithmAsync(GeneticScheduleRequestDto request);
    }

    // ==================== DTOs ====================

    public class GeneticScheduleRequestDto
    {
        public string Semester { get; set; } = "fall";
        public int Year { get; set; }
        public int PopulationSize { get; set; } = 50;
        public int Generations { get; set; } = 100;
        public double MutationRate { get; set; } = 0.1;
        public double CrossoverRate { get; set; } = 0.8;
    }

    public class GeneticScheduleResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int ScheduledCount { get; set; }
        public int Generations { get; set; }
        public double FinalFitness { get; set; }
        public List<ScheduleDto> Schedules { get; set; } = new();
        public List<string> Conflicts { get; set; } = new();
        public GeneticStatsDto Stats { get; set; } = new();
    }

    public class GeneticStatsDto
    {
        public double InitialFitness { get; set; }
        public double FinalFitness { get; set; }
        public int HardConstraintViolations { get; set; }
        public int SoftConstraintScore { get; set; }
        public long ExecutionTimeMs { get; set; }
    }

    // ==================== GENETIC ALGORITHM ====================

    public class GeneticSchedulingService : IGeneticSchedulingService
    {
        private readonly CampusDbContext _context;
        private readonly Random _random = new();

        // Time slots configuration
        private static readonly TimeSpan[] TimeSlots = {
            new(8, 0, 0), new(10, 0, 0), new(12, 0, 0),
            new(14, 0, 0), new(16, 0, 0)
        };

        private static readonly string[] DayNames = { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" };

        public GeneticSchedulingService(CampusDbContext context)
        {
            _context = context;
        }

        public async Task<GeneticScheduleResultDto> GenerateWithGeneticAlgorithmAsync(GeneticScheduleRequestDto request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new GeneticScheduleResultDto();

            try
            {
                // Load data
                var sections = await _context.CourseSections
                    .Include(s => s.Course)
                    .Include(s => s.Instructor)
                    .Where(s => !s.IsDeleted)
                    .ToListAsync();

                var classrooms = await _context.Classrooms.ToListAsync();

                if (!sections.Any() || !classrooms.Any())
                {
                    result.Success = false;
                    result.Message = "No sections or classrooms available";
                    return result;
                }

                // Initialize population
                var population = InitializePopulation(sections, classrooms, request.PopulationSize);
                double initialFitness = population.Max(p => CalculateFitness(p, sections, classrooms));

                // Evolution loop
                for (int gen = 0; gen < request.Generations; gen++)
                {
                    // Evaluate fitness
                    var fitnessScores = population.Select(p => (Chromosome: p, Fitness: CalculateFitness(p, sections, classrooms))).ToList();
                    fitnessScores = fitnessScores.OrderByDescending(f => f.Fitness).ToList();

                    // Selection (Tournament)
                    var newPopulation = new List<Chromosome>();

                    // Elitism - keep top 2
                    newPopulation.Add(fitnessScores[0].Chromosome);
                    if (fitnessScores.Count > 1)
                        newPopulation.Add(fitnessScores[1].Chromosome);

                    // Generate offspring
                    while (newPopulation.Count < request.PopulationSize)
                    {
                        var parent1 = TournamentSelect(fitnessScores);
                        var parent2 = TournamentSelect(fitnessScores);

                        Chromosome child;
                        if (_random.NextDouble() < request.CrossoverRate)
                        {
                            child = Crossover(parent1, parent2);
                        }
                        else
                        {
                            child = parent1.Clone();
                        }

                        if (_random.NextDouble() < request.MutationRate)
                        {
                            Mutate(child, classrooms);
                        }

                        newPopulation.Add(child);
                    }

                    population = newPopulation;
                }

                // Get best solution
                var bestSolution = population
                    .Select(p => (Chromosome: p, Fitness: CalculateFitness(p, sections, classrooms)))
                    .OrderByDescending(f => f.Fitness)
                    .First();

                // Clear existing schedules
                var existingSchedules = await _context.Schedules
                    .Where(s => s.Semester == request.Semester && s.Year == request.Year)
                    .ToListAsync();
                _context.Schedules.RemoveRange(existingSchedules);

                // Create schedules from best chromosome
                var createdSchedules = new List<Schedule>();
                foreach (var gene in bestSolution.Chromosome.Genes)
                {
                    var schedule = new Schedule
                    {
                        SectionId = gene.SectionId,
                        DayOfWeek = gene.DayOfWeek,
                        StartTime = TimeSlots[gene.TimeSlotIndex],
                        EndTime = TimeSlots[gene.TimeSlotIndex].Add(TimeSpan.FromHours(2)),
                        ClassroomId = gene.ClassroomId,
                        Semester = request.Semester,
                        Year = request.Year,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    createdSchedules.Add(schedule);
                    _context.Schedules.Add(schedule);
                }

                await _context.SaveChangesAsync();

                // Load created schedules with relations
                var loadedSchedules = await _context.Schedules
                    .Include(s => s.Section).ThenInclude(sec => sec!.Course)
                    .Include(s => s.Section).ThenInclude(sec => sec!.Instructor)
                    .Include(s => s.Classroom)
                    .Where(s => s.Semester == request.Semester && s.Year == request.Year)
                    .ToListAsync();

                stopwatch.Stop();

                result.Success = true;
                result.ScheduledCount = createdSchedules.Count;
                result.Generations = request.Generations;
                result.FinalFitness = bestSolution.Fitness;
                result.Message = $"Genetic Algorithm completed. Scheduled {createdSchedules.Count} sections.";
                result.Schedules = loadedSchedules.Select(s => new ScheduleDto
                {
                    Id = s.Id,
                    SectionId = s.SectionId,
                    CourseCode = s.Section?.Course?.Code ?? "",
                    CourseName = s.Section?.Course?.Name ?? "",
                    InstructorName = s.Section?.Instructor != null ? $"{s.Section.Instructor.FirstName} {s.Section.Instructor.LastName}" : "",
                    DayOfWeek = s.DayOfWeek,
                    DayName = DayNames[s.DayOfWeek],
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    ClassroomName = s.Classroom?.RoomNumber ?? "",
                    Building = s.Classroom?.Building ?? ""
                }).ToList();

                result.Stats = new GeneticStatsDto
                {
                    InitialFitness = initialFitness,
                    FinalFitness = bestSolution.Fitness,
                    HardConstraintViolations = CountHardConstraintViolations(bestSolution.Chromosome, sections, classrooms),
                    SoftConstraintScore = CalculateSoftConstraints(bestSolution.Chromosome, sections),
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }

            return result;
        }

        // ==================== CHROMOSOME ====================

        private class Chromosome
        {
            public List<Gene> Genes { get; set; } = new();
            public Chromosome Clone() => new() { Genes = Genes.Select(g => new Gene
            {
                SectionId = g.SectionId,
                DayOfWeek = g.DayOfWeek,
                TimeSlotIndex = g.TimeSlotIndex,
                ClassroomId = g.ClassroomId
            }).ToList() };
        }

        private class Gene
        {
            public int SectionId { get; set; }
            public int DayOfWeek { get; set; } // 1-5 (Mon-Fri)
            public int TimeSlotIndex { get; set; } // 0-4
            public int ClassroomId { get; set; }
        }

        // ==================== GA OPERATIONS ====================

        private List<Chromosome> InitializePopulation(List<CourseSection> sections, List<Classroom> classrooms, int size)
        {
            var population = new List<Chromosome>();

            for (int i = 0; i < size; i++)
            {
                var chromosome = new Chromosome();
                foreach (var section in sections)
                {
                    var gene = new Gene
                    {
                        SectionId = section.Id,
                        DayOfWeek = _random.Next(1, 6), // Mon-Fri
                        TimeSlotIndex = _random.Next(0, TimeSlots.Length),
                        ClassroomId = classrooms[_random.Next(classrooms.Count)].Id
                    };
                    chromosome.Genes.Add(gene);
                }
                population.Add(chromosome);
            }

            return population;
        }

        private double CalculateFitness(Chromosome chromosome, List<CourseSection> sections, List<Classroom> classrooms)
        {
            double fitness = 1000; // Start with high fitness

            // Hard constraints (heavy penalty)
            fitness -= CountHardConstraintViolations(chromosome, sections, classrooms) * 100;

            // Soft constraints (bonus)
            fitness += CalculateSoftConstraints(chromosome, sections);

            return Math.Max(0, fitness);
        }

        private int CountHardConstraintViolations(Chromosome chromosome, List<CourseSection> sections, List<Classroom> classrooms)
        {
            int violations = 0;
            var sectionDict = sections.ToDictionary(s => s.Id);
            var classroomDict = classrooms.ToDictionary(c => c.Id);

            // Check each pair of genes
            for (int i = 0; i < chromosome.Genes.Count; i++)
            {
                var gene1 = chromosome.Genes[i];
                var section1 = sectionDict.GetValueOrDefault(gene1.SectionId);

                // Capacity check
                var classroom = classroomDict.GetValueOrDefault(gene1.ClassroomId);
                if (section1 != null && classroom != null && section1.Capacity > classroom.Capacity)
                    violations++;

                for (int j = i + 1; j < chromosome.Genes.Count; j++)
                {
                    var gene2 = chromosome.Genes[j];
                    var section2 = sectionDict.GetValueOrDefault(gene2.SectionId);

                    // Same time slot check
                    if (gene1.DayOfWeek == gene2.DayOfWeek && gene1.TimeSlotIndex == gene2.TimeSlotIndex)
                    {
                        // Classroom conflict
                        if (gene1.ClassroomId == gene2.ClassroomId)
                            violations++;

                        // Instructor conflict
                        if (section1 != null && section2 != null && section1.InstructorId == section2.InstructorId)
                            violations++;
                    }
                }
            }

            return violations;
        }

        private int CalculateSoftConstraints(Chromosome chromosome, List<CourseSection> sections)
        {
            int score = 0;
            var sectionDict = sections.ToDictionary(s => s.Id);

            // Prefer morning slots for lectures (bonus)
            foreach (var gene in chromosome.Genes)
            {
                if (gene.TimeSlotIndex < 2) // Morning slots
                    score += 5;
            }

            // Even distribution across days (bonus)
            var dayDistribution = chromosome.Genes.GroupBy(g => g.DayOfWeek).Select(g => g.Count()).ToList();
            if (dayDistribution.Any())
            {
                var variance = dayDistribution.Select(d => Math.Pow(d - dayDistribution.Average(), 2)).Average();
                if (variance < 2) score += 20;
                else if (variance < 5) score += 10;
            }

            // Minimize gaps for same instructor (bonus)
            var instructorSchedules = chromosome.Genes
                .Select(g => new { g.DayOfWeek, g.TimeSlotIndex, InstructorId = sectionDict.GetValueOrDefault(g.SectionId)?.InstructorId })
                .Where(x => x.InstructorId.HasValue)
                .GroupBy(x => new { x.DayOfWeek, x.InstructorId });

            foreach (var group in instructorSchedules)
            {
                var slots = group.Select(x => x.TimeSlotIndex).OrderBy(x => x).ToList();
                if (slots.Count > 1)
                {
                    bool hasGap = false;
                    for (int i = 1; i < slots.Count; i++)
                    {
                        if (slots[i] - slots[i - 1] > 1) hasGap = true;
                    }
                    if (!hasGap) score += 10;
                }
            }

            return score;
        }

        private Chromosome TournamentSelect(List<(Chromosome Chromosome, double Fitness)> population)
        {
            int tournamentSize = 3;
            var tournament = new List<(Chromosome Chromosome, double Fitness)>();

            for (int i = 0; i < tournamentSize; i++)
            {
                tournament.Add(population[_random.Next(population.Count)]);
            }

            return tournament.OrderByDescending(t => t.Fitness).First().Chromosome;
        }

        private Chromosome Crossover(Chromosome parent1, Chromosome parent2)
        {
            var child = new Chromosome();
            int crossoverPoint = _random.Next(1, parent1.Genes.Count);

            for (int i = 0; i < parent1.Genes.Count; i++)
            {
                var sourceGene = i < crossoverPoint ? parent1.Genes[i] : parent2.Genes[i];
                child.Genes.Add(new Gene
                {
                    SectionId = sourceGene.SectionId,
                    DayOfWeek = sourceGene.DayOfWeek,
                    TimeSlotIndex = sourceGene.TimeSlotIndex,
                    ClassroomId = sourceGene.ClassroomId
                });
            }

            return child;
        }

        private void Mutate(Chromosome chromosome, List<Classroom> classrooms)
        {
            if (!chromosome.Genes.Any()) return;

            int geneIndex = _random.Next(chromosome.Genes.Count);
            var gene = chromosome.Genes[geneIndex];

            // Random mutation type
            switch (_random.Next(3))
            {
                case 0: // Change day
                    gene.DayOfWeek = _random.Next(1, 6);
                    break;
                case 1: // Change time slot
                    gene.TimeSlotIndex = _random.Next(TimeSlots.Length);
                    break;
                case 2: // Change classroom
                    gene.ClassroomId = classrooms[_random.Next(classrooms.Count)].Id;
                    break;
            }
        }
    }
}
