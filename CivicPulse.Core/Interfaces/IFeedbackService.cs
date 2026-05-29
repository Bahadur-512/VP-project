using CivicPulse.Core.DTOs;

namespace CivicPulse.Core.Interfaces;

public interface IFeedbackService
{
    Task<FeedbackDto> CreateAsync(CreateFeedbackDto dto);
    Task<FeedbackDto?> GetByComplaintAsync(int complaintId);
    Task<bool> HasFeedbackAsync(int complaintId, int citizenId);
    Task<List<FeedbackDto>> GetAllAsync();
    Task<double> GetAverageRatingAsync();
    Task<int> GetCountAsync();
}
