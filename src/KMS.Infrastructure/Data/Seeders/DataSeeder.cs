using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using KMS.Domain.Entities.Identity;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Enums;

namespace KMS.Infrastructure.Data.Seeders;

public class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        
        var logger = services.GetRequiredService<ILogger<DataSeeder>>();
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<Role>>();

        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            logger.LogInformation("Starting database seeding...");

            // Seed Roles
            await SeedRolesAsync(roleManager, logger);
            
            // Seed Users
            await SeedUsersAsync(userManager, logger);
            
            // Seed Categories
            await SeedCategoriesAsync(context, logger);
            
            // Seed Tags
            await SeedTagsAsync(context, logger);
            
            // Seed Sample Articles
            await SeedArticlesAsync(context, userManager, logger);

            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager, ILogger logger)
    {
        var roles = new[]
        {
            new { Name = "Admin", Description = "ผู้ดูแลระบบ", Permissions = "[\"*\"]" },
            new { Name = "Faculty", Description = "อาจารย์", Permissions = "[\"articles:write\",\"articles:publish\",\"articles:review\"]" },
            new { Name = "Researcher", Description = "นักวิจัย", Permissions = "[\"articles:write\",\"articles:submit\"]" },
            new { Name = "Student", Description = "นักศึกษา", Permissions = "[\"articles:read\",\"comments:write\"]" },
            new { Name = "Guest", Description = "บุคคลทั่วไป", Permissions = "[\"articles:read:public\"]" }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                var newRole = new Role
                {
                    Name = role.Name,
                    NormalizedName = role.Name.ToUpper(),
                    Description = role.Description,
                    Permissions = role.Permissions
                };
                
                await roleManager.CreateAsync(newRole);
                logger.LogInformation($"Created role: {role.Name}");
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<AppUser> userManager, ILogger logger)
    {
        var users = new[]
        {
            new
            {
                UserName = "denpha",
                Email = "denpha.sa@rmuti.ac.th",
                FullNameTh = "เด่นภา แสนเพียง",
                FullNameEn = "Denpha Saenpiang",
                Faculty = "สำนักงานวิทยาเขต",
                Department = "แผนกงานวิทยบริการและเทคโนโลยีสารสนเทศ",
                Position = "ผู้ดูแลระบบ",
                EmployeeCode = "ADM001",
                Password = "Denpha_@$&2022",
                Roles = new[] { "Admin" }
            },
            new
            {
                UserName = "somsak@rmuti.ac.th",
                Email = "somsak@rmuti.ac.th",
                FullNameTh = "สมศักดิ์ จันทร์แจ่ม",
                FullNameEn = "Somsak Chanjam",
                Faculty = "คณะวิศวกรรมศาสตร์",
                Department = "ภาควิชาวิศวกรรมคอมพิวเตอร์",
                Position = "อาจารย์",
                EmployeeCode = "FAC001",
                Password = "Faculty@1234",
                Roles = new[] { "Faculty" }
            },
            new
            {
                UserName = "pornchai@rmuti.ac.th",
                Email = "pornchai@rmuti.ac.th",
                FullNameTh = "พรชัย ศรีสุข",
                FullNameEn = "Pornchai Srisuk",
                Faculty = "คณะวิทยาศาสตร์และเทคโนโลยี",
                Department = "ภาควิชาวิทยาการคอมพิวเตอร์",
                Position = "นักวิจัย",
                EmployeeCode = "RES001",
                Password = "Researcher@1234",
                Roles = new[] { "Researcher" }
            },
            new
            {
                UserName = "nattapong@rmuti.ac.th",
                Email = "nattapong@rmuti.ac.th",
                FullNameTh = "ณัฐพงศ์ ใจดี",
                FullNameEn = "Nattapong Jaidee",
                Faculty = "คณะวิศวกรรมศาสตร์",
                Department = "ภาควิชาวิศวกรรมคอมพิวเตอร์",
                Position = "นักศึกษา",
                EmployeeCode = "STU001",
                Password = "Student@1234",
                Roles = new[] { "Student" }
            }
        };

        foreach (var userData in users)
        {
            var user = await userManager.FindByEmailAsync(userData.Email);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName = userData.UserName,
                    Email = userData.Email,
                    EmailConfirmed = true,
                    FullNameTh = userData.FullNameTh,
                    FullNameEn = userData.FullNameEn,
                    Faculty = userData.Faculty,
                    Department = userData.Department,
                    Position = userData.Position,
                    EmployeeCode = userData.EmployeeCode,
                    Bio = "ผู้ใช้งานระบบ KMS",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    logger.LogInformation($"Created user: {userData.UserName}");
                    
                    // Assign roles
                    foreach (var role in userData.Roles)
                    {
                        await userManager.AddToRoleAsync(user, role);
                        logger.LogInformation($"Assigned role {role} to user {userData.UserName}");
                    }
                }
                else
                {
                    logger.LogError($"Failed to create user {userData.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.Categories.AnyAsync())
        {
            var categories = new[]
            {
                new Category
                {
                    Name = "งานวิจัย",
                    NameEn = "Research",
                    Slug = "research",
                    Description = "ผลงานวิจัยและวิทยานิพนธ์",
                    IconName = "research",
                    ColorHex = "#3B82F6",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                },
                new Category
                {
                    Name = "สื่อการสอน",
                    NameEn = "Teaching Materials",
                    Slug = "teaching-materials",
                    Description = "สื่อการสอนและเอกสารประกอบการเรียน",
                    IconName = "book",
                    ColorHex = "#10B981",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                },
                new Category
                {
                    Name = "บทความวิชาการ",
                    NameEn = "Academic Articles",
                    Slug = "academic-articles",
                    Description = "บทความทางวิชาการและวารสาร",
                    IconName = "article",
                    ColorHex = "#8B5CF6",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                },
                new Category
                {
                    Name = "นโยบายและแนวปฏิบัติ",
                    NameEn = "Policies & Guidelines",
                    Slug = "policies-guidelines",
                    Description = "นโยบายและแนวปฏิบัติของมหาวิทยาลัย",
                    IconName = "policy",
                    ColorHex = "#F59E0B",
                    SortOrder = 4,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                },
                new Category
                {
                    Name = "ความรู้ทั่วไป",
                    NameEn = "General Knowledge",
                    Slug = "general-knowledge",
                    Description = "ความรู้ทั่วไปและสาระน่ารู้",
                    IconName = "knowledge",
                    ColorHex = "#EF4444",
                    SortOrder = 5,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
            
            logger.LogInformation($"Created {categories.Length} categories");
        }
    }

    private static async Task SeedTagsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.Tags.AnyAsync())
        {
            var tags = new[]
            {
                new Tag { Name = "เทคโนโลยี", Slug = "technology", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "การศึกษา", Slug = "education", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "วิจัย", Slug = "research", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "นวัตกรรม", Slug = "innovation", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "ดิจิทัล", Slug = "digital", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "AI", Slug = "ai", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "ข้อมูล", Slug = "data", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "การเรียนรู้", Slug = "learning", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "พัฒนาการศึกษา", Slug = "education-development", CreatedAt = DateTime.UtcNow },
                new Tag { Name = "อนาคต", Slug = "future", CreatedAt = DateTime.UtcNow }
            };

            await context.Tags.AddRangeAsync(tags);
            await context.SaveChangesAsync();
            
            logger.LogInformation($"Created {tags.Length} tags");
        }
    }

    private static async Task SeedArticlesAsync(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger logger)
    {
        if (!await context.KnowledgeArticles.AnyAsync())
        {
            var admin = await userManager.FindByEmailAsync("denpha.sa@rmuti.ac.th");
            var faculty = await userManager.FindByEmailAsync("somsak@rmuti.ac.th");
            var researcher = await userManager.FindByEmailAsync("pornchai@rmuti.ac.th");
            
            var researchCategory = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "research");
            var teachingCategory = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "teaching-materials");
            
            var aiTag = await context.Tags.FirstOrDefaultAsync(t => t.Slug == "ai");
            var technologyTag = await context.Tags.FirstOrDefaultAsync(t => t.Slug == "technology");
            var educationTag = await context.Tags.FirstOrDefaultAsync(t => t.Slug == "education");

            var articles = new[]
            {
                new KnowledgeArticle
                {
                    Title = "การประยุกต์ใช้ AI ในการจัดการองค์ความรู้",
                    TitleEn = "Application of AI in Knowledge Management",
                    Slug = "ai-knowledge-management",
                    Content = @"# การประยุกต์ใช้ AI ในการจัดการองค์ความรู้

ระบบจัดการองค์ความรู้แบบดั้งเดิมมักประสบปัญหาด้วยข้อมูลที่มีปริมาณมากและความซับซ้อน AI สามารถช่วยแก้ปัญหาเหล่านี้ได้หลายวิธี:

## 1. การจัดหมวดหมู่อัตโนมัติ
- ใช้ NLP วิเคราะห์เนื้อหาและจัดหมวดหมู่
- ลดเวลาการจัดกลุ่มข้อมูลด้วยมือ

## 2. การค้นหาอัจฉริยะ
- Semantic search ด้วย vector embedding
- ค้นหาจากความหมายไม่ใช่แค่คำค้น

## 3. การแนะนำเนื้อหา
- Recommender systems สำหรับผู้ใช้
- แนะนำบทความที่เกี่ยวข้อง

## ประโยชน์ที่ได้รับ
- เพิ่มประสิทธิภาพการค้นหา 40%
- ลดเวลาจัดการข้อมูล 60%
- เพิ่มความพึงพอใจผู้ใช้",

                    Summary = "บทความนี้กล่าวถึงการประยุกต์ใช้ AI ในการจัดการองค์ความรู้ภายในองค์กร",
                    SummaryEn = "This article discusses the application of AI in knowledge management within organizations",
                    KeywordsEn = "AI, Knowledge Management, Machine Learning, NLP",
                    Status = ArticleStatus.Published,
                    Visibility = Visibility.Internal,
                    CategoryId = researchCategory?.Id ?? Guid.Empty,
                    AuthorId = faculty?.Id ?? Guid.Empty,
                    ReviewerId = admin?.Id,
                    IsAutoTranslated = false,
                    ViewCount = 45,
                    LikeCount = 12,
                    PublishedAt = DateTime.UtcNow.AddDays(-30),
                    CreatedAt = DateTime.UtcNow.AddDays(-45),
                    CreatedBy = "system"
                },
                new KnowledgeArticle
                {
                    Title = "การพัฒนาระบบ e-Learning แบบ Interactive",
                    TitleEn = "Development of Interactive e-Learning Systems",
                    Slug = "interactive-elearning-development",
                    Content = @"# การพัฒนาระบบ e-Learning แบบ Interactive

ในยุคดิจิทัล การเรียนรู้ผ่านระบบ e-Learning แบบ interactive มีความสำคัญมากขึ้น

## คุณสมบัติสำคัญ
1. **Interactive Content**
   - วีดิโอแบบ interactive
   - แบบฝึกหัดทันที
   - เกมการเรียนรู้

2. **Personalization**
   - Adaptive learning paths
   - Personalized recommendations

3. **Collaboration Features**
   - Discussion forums
   - Group projects
   - Peer review

## เทคโนโลยีที่ใช้
- HTML5, CSS3, JavaScript
- WebRTC สำหรับ video conferencing
- WebSockets สำหรับ real-time interaction

## ผลการศึกษา
- เพิ่ม engagement 35%
- เพิ่ม retention rate 25%
- ความพึงพอใจผู้เรียน 4.5/5",

                    Summary = "บทความเกี่ยวกับการพัฒนาระบบ e-Learning แบบ interactive สำหรับการศึกษาในยุคดิจิทัล",
                    SummaryEn = "Article about developing interactive e-Learning systems for digital age education",
                    KeywordsEn = "e-Learning, Interactive Learning, Educational Technology, Digital Education",
                    Status = ArticleStatus.Published,
                    Visibility = Visibility.Public,
                    CategoryId = teachingCategory?.Id ?? Guid.Empty,
                    AuthorId = researcher?.Id ?? Guid.Empty,
                    ReviewerId = faculty?.Id,
                    IsAutoTranslated = false,
                    ViewCount = 89,
                    LikeCount = 24,
                    PublishedAt = DateTime.UtcNow.AddDays(-15),
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    CreatedBy = "system"
                }
            };

            await context.KnowledgeArticles.AddRangeAsync(articles);
            await context.SaveChangesAsync();
            
            // Add tags to articles
            var article1 = await context.KnowledgeArticles.FirstOrDefaultAsync(a => a.Slug == "ai-knowledge-management");
            var article2 = await context.KnowledgeArticles.FirstOrDefaultAsync(a => a.Slug == "interactive-elearning-development");
            
            if (article1 != null && aiTag != null && technologyTag != null)
            {
                article1.ArticleTags.Add(new ArticleTag { ArticleId = article1.Id, TagId = aiTag.Id });
                article1.ArticleTags.Add(new ArticleTag { ArticleId = article1.Id, TagId = technologyTag.Id });
            }
            
            if (article2 != null && educationTag != null && technologyTag != null)
            {
                article2.ArticleTags.Add(new ArticleTag { ArticleId = article2.Id, TagId = educationTag.Id });
                article2.ArticleTags.Add(new ArticleTag { ArticleId = article2.Id, TagId = technologyTag.Id });
            }
            
            await context.SaveChangesAsync();
            
            logger.LogInformation($"Created {articles.Length} sample articles");
        }
    }
}