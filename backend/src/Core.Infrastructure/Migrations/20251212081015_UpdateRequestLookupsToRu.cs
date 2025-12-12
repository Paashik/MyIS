using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRequestLookupsToRu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE requests.request_statuses SET name = 'Черновик' WHERE code = 'Draft';
UPDATE requests.request_statuses SET name = 'На согласовании' WHERE code = 'Submitted';
UPDATE requests.request_statuses SET name = 'На рассмотрении' WHERE code = 'InReview';
UPDATE requests.request_statuses SET name = 'Согласована' WHERE code = 'Approved';
UPDATE requests.request_statuses SET name = 'Отклонена' WHERE code = 'Rejected';
UPDATE requests.request_statuses SET name = 'В работе' WHERE code = 'InWork';
UPDATE requests.request_statuses SET name = 'Выполнена' WHERE code = 'Done';
UPDATE requests.request_statuses SET name = 'Закрыта' WHERE code = 'Closed';

UPDATE requests.request_types SET name = 'Заявка на обеспечение' WHERE code = 'SupplyRequest';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE requests.request_statuses SET name = 'Draft' WHERE code = 'Draft';
UPDATE requests.request_statuses SET name = 'Submitted' WHERE code = 'Submitted';
UPDATE requests.request_statuses SET name = 'In review' WHERE code = 'InReview';
UPDATE requests.request_statuses SET name = 'Approved' WHERE code = 'Approved';
UPDATE requests.request_statuses SET name = 'Rejected' WHERE code = 'Rejected';
UPDATE requests.request_statuses SET name = 'In work' WHERE code = 'InWork';
UPDATE requests.request_statuses SET name = 'Done' WHERE code = 'Done';
UPDATE requests.request_statuses SET name = 'Closed' WHERE code = 'Closed';

UPDATE requests.request_types SET name = 'Supply request' WHERE code = 'SupplyRequest';
");
        }
    }
}
