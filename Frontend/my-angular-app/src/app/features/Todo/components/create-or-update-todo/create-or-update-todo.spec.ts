import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateOrUpdateTodo } from './create-or-update-todo';

describe('CreateOrUpdateTodo', () => {
  let component: CreateOrUpdateTodo;
  let fixture: ComponentFixture<CreateOrUpdateTodo>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateOrUpdateTodo],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateOrUpdateTodo);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
