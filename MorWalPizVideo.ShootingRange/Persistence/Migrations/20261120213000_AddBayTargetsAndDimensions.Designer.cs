using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MorWalPizVideo.ShootingRange.Repositories;

#nullable disable

namespace MorWalPizVideo.ShootingRange.Persistence.Migrations;

[DbContext(typeof(ShootingRangeDbContext))]
[Migration("20261120213000_AddBayTargetsAndDimensions")]
public partial class AddBayTargetsAndDimensions
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        ShootingRangeDbContextModelSnapshot.ConfigureTargetModel(modelBuilder);
}
