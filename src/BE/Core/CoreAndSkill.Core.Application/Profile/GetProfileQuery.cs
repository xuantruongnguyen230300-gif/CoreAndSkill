using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Profile;

// GET /api/v1/core/profile — docs/contracts/profile.md §1. Luôn là hồ sơ của chính người gọi.
public sealed record GetProfileQuery : IQuery<ProfileDto>;
