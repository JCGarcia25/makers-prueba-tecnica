export type Role = 'User' | 'Admin';
export type LoanStatus = 'Pending' | 'Approved' | 'Rejected';
export interface CurrentUser { id: string; email: string; name: string; role: Role; }
export interface TokenResponse { accessToken: string; expiresAt: string; user: CurrentUser; }
export interface LoanDecision { administratorId: string; status: LoanStatus; reason: string; decidedAt: string; }
export interface Loan { id: string; userId: string; amount: number; termMonths: number; status: LoanStatus; createdAt: string; updatedAt: string; version: string; decisions: LoanDecision[]; }
export interface Page<T> { items: T[]; total: number; pageNumber: number; pageSize: number; }
export interface ProblemDetails { title?: string; detail?: string; }
