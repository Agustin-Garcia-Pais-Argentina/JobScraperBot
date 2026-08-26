using JobScraperBot.Scrapers.Helpers;
using Xunit;

namespace JobScraperBot.Scrapers.Tests
{
    public class ExternalIdGeneratorTests
    {
        [Fact]
        public void ReturnsOfficialId_WhenProvided()
        {
            var id = ExternalIdGenerator.Generate("SiteA", "official-123", "https://foo/bar", "Title", "Company", "Location");
            Assert.Equal("official-123", id);
        }

        [Fact]
        public void UsesSlugFromUrl_WhenUrlHasSlug()
        {
            var id = ExternalIdGenerator.Generate("SiteA", null, "https://example.com/jobs/slug-123?utm=1#frag", "Title", "Company", "Location");
            Assert.Equal("slug-123", id);
        }

        [Fact]
        public void HandlesUrlMissingScheme_AndUsesSlug()
        {
            var id = ExternalIdGenerator.Generate("SiteA", null, "example.com/jobs/another-slug/?q=1", "Title", "Company", "Location");
            Assert.Equal("another-slug", id);
        }

        [Fact]
        public void UsesHash_WhenUrlHasNoUsefulSlug_AndIsStable()
        {
            var id1 = ExternalIdGenerator.Generate("SiteA", null, "https://example.com/", "Title", "Company", "Location");
            var id2 = ExternalIdGenerator.Generate("SiteA", null, "https://example.com/", "Title", "Company", "Location");

            Assert.NotNull(id1);
            Assert.Equal(24, id1.Length);
            Assert.Equal(id1, id2); // estable entre ejecuciones
        }

        [Fact]
        public void FallbackHash_FromCompositeFields_IsStableAndDifferentForDifferentInputs()
        {
            var idA1 = ExternalIdGenerator.Generate("SiteA", null, null, "Title A", "Company", "Loc");
            var idA2 = ExternalIdGenerator.Generate("SiteA", null, null, "Title A", "Company", "Loc");
            var idB = ExternalIdGenerator.Generate("SiteA", null, null, "Another Title", "Company", "Loc");

            Assert.Equal(24, idA1.Length);
            Assert.Equal(idA1, idA2);
            Assert.NotEqual(idA1, idB);
        }
    }
}
