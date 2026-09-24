import { describe, expect, it } from "vitest";
import { ApiResponseError, requireSuccessfulResponse } from "./apiService";

describe("requireSuccessfulResponse", () => {
  it("accepts a successful response", () => {
    const response = { status: 204 };

    expect(requireSuccessfulResponse(response)).toBe(response);
  });

  it("rejects failed responses and preserves field errors", () => {
    expect(() =>
      requireSuccessfulResponse({
        status: 400,
        errors: ["The Email field is not a valid e-mail address."],
        fieldErrors: {
          Email: ["The Email field is not a valid e-mail address."],
          Token: ["The Token field is required."],
        },
      }),
    ).toThrow(
      "Email: The Email field is not a valid e-mail address.\nToken: The Token field is required.",
    );

    try {
      requireSuccessfulResponse({
        status: 400,
        errors: ["validation failed"],
        fieldErrors: { Email: ["invalid"] },
      });
    } catch (error) {
      expect(error).toBeInstanceOf(ApiResponseError);
      expect((error as ApiResponseError).status).toBe(400);
      expect((error as ApiResponseError).fieldErrors).toEqual({
        Email: ["invalid"],
      });
    }
  });
});
