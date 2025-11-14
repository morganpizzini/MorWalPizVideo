import { ActionFunctionArgs, redirect } from 'react-router';

/**
 * Action function for the video edit route
 * Handles form submission for updating video information
 */
export const action = async ({ request, params }: ActionFunctionArgs) => {
  const { id } = params;
  
  if (!id) {
    throw new Response('Video ID is required', { status: 400 });
  }

  try {
    const formData = await request.formData();
    
    const updateData = {
      id,
      title: formData.get('title') as string,
      description: formData.get('description') as string,
      url: formData.get('url') as string,
      category: formData.get('category') as string,
      matchType: parseInt(formData.get('matchType') as string),
      thumbnailVideoId: formData.get('thumbnailVideoId') as string,
    };

    // TODO: Implement actual API call to update the video
    // For now, we'll just log the data and redirect
    console.log('Update video data:', updateData);
    
    // Simulate API call
    const response = await fetch(`/api/videos/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(updateData),
    });

    if (!response.ok) {
      throw new Error(`Failed to update video: ${response.statusText}`);
    }

    // Redirect to the detail page after successful update
    return redirect(`/videos/${id}`);
    
  } catch (error) {
    console.error('Error updating video:', error);
    // For now, return the error. In a real app, you'd want better error handling
    throw new Response('Failed to update video', { status: 500 });
  }
};
