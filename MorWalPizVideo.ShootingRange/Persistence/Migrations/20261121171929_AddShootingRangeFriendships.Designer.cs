using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MorWalPizVideo.ShootingRange.Repositories;

#nullable disable

namespace MorWalPizVideo.ShootingRange.Persistence.Migrations;

[DbContext(typeof(ShootingRangeDbContext))]
[Migration("20261121171929_AddShootingRangeFriendships")]
public partial class AddShootingRangeFriendships
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        ShootingRangeDbContextModelSnapshot.ConfigureTargetModel(modelBuilder);
}
